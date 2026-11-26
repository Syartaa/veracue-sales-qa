using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;
using VeraCue.Api.DTOs.Calls;
using VeraCue.Api.Entities;
using VeraCue.Api.Services.Scoring;
using VeraCue.Api.Services.Storage;

namespace VeraCue.Api.Services.Calls;

public enum RetryResult { Retried, NotFound, NotFailed }

/// <summary>Uploading calls, reading their status, and retrying failed processing.</summary>
public sealed class CallService(
    AppDbContext db,
    AudioStorage storage,
    CallProcessingQueue queue,
    TimeProvider time,
    ILogger<CallService> logger)
{
    public Task<bool> IsActiveTemplateAsync(Guid templateId, CancellationToken ct) =>
        db.QaTemplates.AnyAsync(t => t.Id == templateId && !t.IsArchived, ct);

    /// <summary>
    /// Stores the recording privately, records the call as "Uploaded" and queues it.
    /// Returns immediately; transcription happens in the background worker.
    /// Expects a request that already passed CallUploadValidator.
    /// </summary>
    public async Task<CallDetailDto> UploadAsync(UploadCallRequest request, Guid uploaderId, CancellationToken ct)
    {
        var file = request.File!;
        var now = time.GetUtcNow().UtcDateTime;
        var extension = Path.GetExtension(file.FileName);

        var call = new Call
        {
            TemplateId = request.TemplateId!.Value,
            UploadedByUserId = uploaderId,
            Title = CallUploadValidator.ResolveTitle(request.Title, file.FileName),
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = CallUploadValidator.AllowedExtensions[extension],
            FileSizeBytes = file.Length,
            AudioStorageKey = "", // set below, once the id is known
            Status = CallStatus.Uploaded,
            ProcessingPermissionConfirmedAt = now, // validator guarantees the box was ticked
            UploadedAt = now,
            UpdatedAt = now,
        };
        call.AudioStorageKey = storage.KeyForCall(call.Id, file.FileName);

        // 1. The file first: a call row must never point at a recording that doesn't exist.
        await using (var content = file.OpenReadStream())
        {
            await storage.SaveAsync(call.AudioStorageKey, content, ct);
        }

        // 2. Then the database row. If that fails, remove the file so nothing is orphaned.
        try
        {
            db.Calls.Add(call);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await storage.DeleteAsync(call.AudioStorageKey, CancellationToken.None);
            throw;
        }

        // 3. Hand it to the background worker and answer the browser straight away.
        queue.Enqueue(call.Id);
        logger.LogInformation("Call {CallId} uploaded ({Bytes} bytes) and queued", call.Id, call.FileSizeBytes);

        return (await GetAsync(call.Id, ct))!;
    }

    /// <summary>
    /// Call History: newest first, with optional title search, status and template filters,
    /// and pagination. Scores are calculated (never stored) for the calls on the current page.
    /// </summary>
    public async Task<CallListDto> ListAsync(CallListQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var calls = db.Calls.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_")}%";
            calls = calls.Where(c => EF.Functions.ILike(c.Title, pattern, @"\"));
        }

        if (query.TemplateId is { } templateId)
            calls = calls.Where(c => c.TemplateId == templateId);

        if (query.Status == "InProgress")
        {
            CallStatus[] inProgress = [CallStatus.Uploaded, CallStatus.Transcribing, CallStatus.Transcribed, CallStatus.Evaluating];
            calls = calls.Where(c => inProgress.Contains(c.Status));
        }
        else if (Enum.TryParse<CallStatus>(query.Status, ignoreCase: true, out var status))
        {
            calls = calls.Where(c => c.Status == status);
        }

        var total = await calls.CountAsync(ct);
        var rows = await calls
            .OrderByDescending(c => c.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new { c.Id, c.Title, c.TemplateId, TemplateName = c.Template.Name, c.DurationMs, c.Status, c.FailedStage, c.UploadedAt })
            .ToListAsync(ct);

        // One query for the results of all calls on this page, then the calculator per call.
        var ids = rows.Select(r => r.Id).ToList();
        var resultsByCall = (await db.CriterionResults.AsNoTracking()
                .Where(r => ids.Contains(r.Evaluation.CallId))
                .Select(r => new { r.Evaluation.CallId, Input = new ScoreInput(r.MaxPoints, r.AiOutcome, r.AiScore, r.FinalOutcome, r.FinalScore) })
                .ToListAsync(ct))
            .GroupBy(x => x.CallId)
            .ToDictionary(g => g.Key, g => ScoreCalculator.Calculate(g.Select(x => x.Input).ToList()));

        var items = rows.Select(r =>
        {
            var score = resultsByCall.GetValueOrDefault(r.Id);
            return new CallListItemDto(r.Id, r.Title, r.TemplateId, r.TemplateName, r.DurationMs, r.Status, r.FailedStage,
                score?.Ai.Percent, score?.Current.Percent, score?.OverrideCount ?? 0, score?.PendingCount ?? 0, r.UploadedAt);
        }).ToList();

        return new CallListDto(items, total, page, pageSize);
    }

    /// <summary>
    /// Overview numbers. The average uses REVIEWED calls only (as labelled in the UI): a score is
    /// only trustworthy once a manager has checked it. Scores are calculated, never stored.
    /// </summary>
    public async Task<OverviewDto> OverviewAsync(CancellationToken ct)
    {
        var evaluated = await db.Calls.CountAsync(
            c => c.Status == CallStatus.ReadyForReview || c.Status == CallStatus.Reviewed, ct);
        var pending = await db.Calls.CountAsync(c => c.Status == CallStatus.ReadyForReview, ct);
        var templates = await db.QaTemplates.CountAsync(t => !t.IsArchived, ct);

        var reviewedPercents = (await db.CriterionResults.AsNoTracking()
                .Where(r => r.Evaluation.Call.Status == CallStatus.Reviewed)
                .Select(r => new { r.Evaluation.CallId, Input = new ScoreInput(r.MaxPoints, r.AiOutcome, r.AiScore, r.FinalOutcome, r.FinalScore) })
                .ToListAsync(ct))
            .GroupBy(x => x.CallId)
            .Select(g => ScoreCalculator.Calculate(g.Select(x => x.Input).ToList()).Current.Percent)
            .OfType<int>() // skip calls where nothing was scorable
            .ToList();
            

        int? average = reviewedPercents.Count == 0
            ? null
            : (int)Math.Round(reviewedPercents.Average(), MidpointRounding.AwayFromZero);

        var recent = await ListAsync(new CallListQuery { Page = 1, PageSize = 4 }, ct);
        return new OverviewDto(evaluated, average, pending, templates, recent.Items);
    }

    /// <summary>A call's details, plus its calculated scores once it has been evaluated.</summary>
    public async Task<CallDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var call = await GetBasicAsync(id, ct);
        if (call is null) return null;

        var inputs = await db.CriterionResults.AsNoTracking()
            .Where(r => r.Evaluation.CallId == id)
            .Select(r => new ScoreInput(r.MaxPoints, r.AiOutcome, r.AiScore, r.FinalOutcome, r.FinalScore))
            .ToListAsync(ct);
        if (inputs.Count == 0) return call;

        var score = ScoreCalculator.Calculate(inputs);
        return call with
        {
            AiScorePercent = score.Ai.Percent,
            CurrentScorePercent = score.Current.Percent,
            PendingCount = score.PendingCount,
        };
    }

    private Task<CallDetailDto?> GetBasicAsync(Guid id, CancellationToken ct) =>
        db.Calls.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CallDetailDto(
                c.Id,
                c.Title,
                c.TemplateId,
                c.Template.Name,
                c.Status,
                c.FailedStage,
                c.ErrorMessage,
                c.OriginalFileName,
                c.FileSizeBytes,
                c.DurationMs,
                c.Segments.Count,
                c.Segments.Select(s => s.Speaker).Distinct().Count(),
                c.UploadedAt,
                c.UpdatedAt))
            .FirstOrDefaultAsync(ct);

    /// <summary>Where the call's recording is stored and its type, or null if the call or file doesn't exist.</summary>
    public async Task<(string FullPath, string ContentType)?> GetAudioAsync(Guid id, CancellationToken ct)
    {
        var audio = await db.Calls.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.AudioStorageKey, c.ContentType })
            .FirstOrDefaultAsync(ct);

        if (audio is null || !storage.Exists(audio.AudioStorageKey)) return null;
        return (storage.GetFullPath(audio.AudioStorageKey), audio.ContentType);
    }

    /// <summary>
    /// Restarts a failed call at the stage that failed: a failed transcription goes back to
    /// "Uploaded"; a failed evaluation (Phase 4) goes back to "Transcribed" and keeps the transcript.
    /// </summary>
    public async Task<RetryResult> RetryAsync(Guid id, CancellationToken ct)
    {
        var call = await db.Calls.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (call is null) return RetryResult.NotFound;
        if (call.Status != CallStatus.Failed) return RetryResult.NotFailed;

        call.Status = call.FailedStage == ProcessingStage.Evaluation ? CallStatus.Transcribed : CallStatus.Uploaded;
        call.FailedStage = null;
        call.ErrorMessage = null;
        call.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        queue.Enqueue(call.Id);
        return RetryResult.Retried;
    }
}
