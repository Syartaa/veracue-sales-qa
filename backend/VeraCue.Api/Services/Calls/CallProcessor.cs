using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;
using VeraCue.Api.Entities;
using VeraCue.Api.Services.Evaluation;
using VeraCue.Api.Services.Storage;
using VeraCue.Api.Services.Transcription;
using SegmentEntity = VeraCue.Api.Entities.TranscriptSegment;
using SegmentInput = VeraCue.Api.Services.Transcription.TranscriptSegment;

namespace VeraCue.Api.Services.Calls;

/// <summary>
/// Moves ONE call forward from its current status. Every stage saves its result before the
/// next starts, so a failure never means starting over:
///   Uploaded    -> Transcribing -> Transcribed     (Deepgram)
///   Transcribed -> Evaluating   -> ReadyForReview  (Groq)
/// A failed evaluation keeps the transcript, so "Retry" only re-runs the AI step.
/// </summary>
public sealed class CallProcessor(
    AppDbContext db,
    AudioStorage storage,
    DeepgramTranscriber deepgram,
    GroqEvaluator groq,
    TimeProvider time,
    ILogger<CallProcessor> logger)
{
    public async Task ProcessAsync(Guid callId, CancellationToken ct)
    {
        var call = await db.Calls.FirstOrDefaultAsync(c => c.Id == callId, ct);
        if (call is null) return;

        if (call.Status == CallStatus.Uploaded)
            await TranscribeAsync(call, ct);

        if (call.Status == CallStatus.Transcribed)
            await EvaluateAsync(call, ct);
    }

    // =====================================================================
    // Stage 1: transcription
    // =====================================================================

    private async Task TranscribeAsync(Call call, CancellationToken ct)
    {
        await SetStatusAsync(call, CallStatus.Transcribing, ct);
        logger.LogInformation("Transcribing call {CallId}", call.Id);

        try
        {
            TranscriptionResult result;
            await using (var audio = storage.OpenRead(call.AudioStorageKey))
            {
                result = await deepgram.TranscribeAsync(audio, call.ContentType, ct);
            }

            if (result.Segments.Count == 0)
            {
                await FailAsync(call, ProcessingStage.Transcription, "No speech was detected in the recording.", ct);
                return;
            }

            // Transcript + new status in one transaction. Old segments go first, in case an
            // earlier attempt half-finished (the unique index forbids duplicates anyway).
            await db.TranscriptSegments.Where(s => s.CallId == call.Id).ExecuteDeleteAsync(ct);
            db.TranscriptSegments.AddRange(result.Segments.Select(s => new SegmentEntity
            {
                CallId = call.Id,
                SegmentIndex = s.Index,
                Speaker = (short)s.Speaker,
                StartMs = s.StartMs,
                EndMs = Math.Max(s.EndMs, s.StartMs),
                Text = s.Text,
                Confidence = s.Confidence is { } c ? (float)Math.Clamp(c, 0, 1) : null,
            }));

            call.DurationMs = result.DurationMs > 0 ? result.DurationMs : null;
            call.TranscriptionModel = result.Model;
            call.Status = CallStatus.Transcribed;
            call.FailedStage = null;
            call.ErrorMessage = null;
            call.UpdatedAt = Now;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Call {CallId} transcribed: {Count} segments", call.Id, result.Segments.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // shutting down: the worker recovers "Transcribing" calls on the next start
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transcription failed for call {CallId}", call.Id);
            await FailAsync(call, ProcessingStage.Transcription, FriendlyMessage(ex, "transcription"), ct);
        }
    }

    // =====================================================================
    // Stage 2: AI evaluation
    // =====================================================================

    private async Task EvaluateAsync(Call call, CancellationToken ct)
    {
        await SetStatusAsync(call, CallStatus.Evaluating, ct);
        logger.LogInformation("Evaluating call {CallId}", call.Id);

        try
        {
            // The template as it is NOW is what this call is scored against; it's copied
            // into the results (snapshot), so later template edits never change this evaluation.
            var template = await db.QaTemplates.AsNoTracking()
                .Include(t => t.Criteria)
                .FirstAsync(t => t.Id == call.TemplateId, ct);
            var criteria = template.Criteria.OrderBy(c => c.SortOrder).ToList();

            var segments = await db.TranscriptSegments.AsNoTracking()
                .Where(s => s.CallId == call.Id)
                .OrderBy(s => s.SegmentIndex)
                .ToListAsync(ct);

            // The AI sees short ids ("c1", "c2" …) and segment numbers, which it can echo back
            // reliably; we map them back to database ids below.
            var criterionInputs = criteria
                .Select((c, i) => new CriterionInput($"c{i + 1}", c.Name, c.Instructions, c.MaxPoints))
                .ToList();
            var segmentInputs = segments
                .Select(s => new SegmentInput(s.SegmentIndex, s.Speaker, s.StartMs, s.EndMs, s.Text, s.Confidence))
                .ToList();

            var ai = await EvaluateWithRateLimitRetryAsync(call.Id, segmentInputs, criterionInputs, ct);

            // Log what the AI got wrong (useful for the write-up), then clean it up.
            foreach (var issue in AiResultValidator.Validate(ai.Evaluation, criterionInputs, segments.Count))
                logger.LogWarning("Call {CallId}: AI response issue: {Issue}", call.Id, issue);

            var speakers = segments.Select(s => (int)s.Speaker).Distinct().ToList();
            var (results, agentSpeaker) = EvaluationSanitizer.Sanitize(ai.Evaluation, criterionInputs, segments.Count, speakers);

            // Save evaluation + results + evidence + status in ONE transaction.
            var segmentIdByIndex = segments.ToDictionary(s => s.SegmentIndex, s => s.Id);
            var evaluation = new Entities.Evaluation
            {
                CallId = call.Id,
                TemplateNameSnapshot = template.Name,
                ModelName = ai.Model,
                PromptVersion = ai.PromptVersion,
                CreatedAt = Now,
                Results = results.Select(r =>
                {
                    var criterion = criteria[r.CriterionIndex];
                    var result = new CriterionResult
                    {
                        CriterionId = criterion.Id,
                        SortOrder = r.CriterionIndex,
                        NameSnapshot = criterion.Name,
                        InstructionsSnapshot = criterion.Instructions,
                        MaxPoints = criterion.MaxPoints,
                        AiOutcome = r.Outcome,
                        AiScore = r.Score,
                        AiExplanation = r.Explanation,
                    };
                    result.Evidence = r.EvidenceSegments
                        .Select(index => new ResultEvidence { CriterionResultId = result.Id, TranscriptSegmentId = segmentIdByIndex[index] })
                        .ToList();
                    return result;
                }).ToList(),
            };

            // One evaluation per call: clear any leftover from an earlier attempt first.
            await db.Evaluations.Where(e => e.CallId == call.Id).ExecuteDeleteAsync(ct);
            db.Evaluations.Add(evaluation);

            call.AgentSpeaker = agentSpeaker;
            call.Status = CallStatus.ReadyForReview;
            call.FailedStage = null;
            call.ErrorMessage = null;
            call.UpdatedAt = Now;
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Call {CallId} evaluated with {Model} ({Prompt}): {Count} criteria, {Downgraded} downgraded to NeedsReview, {Tokens} tokens",
                call.Id, ai.Model, ai.PromptVersion, results.Count, results.Count(r => r.Downgraded),
                (ai.PromptTokens ?? 0) + (ai.CompletionTokens ?? 0));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // the worker recovers "Evaluating" calls on the next start
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Evaluation failed for call {CallId}", call.Id);
            await FailAsync(call, ProcessingStage.Evaluation, FriendlyMessage(ex, "AI evaluation"), ct);
        }
    }

    /// <summary>
    /// Groq's free tier allows a limited number of tokens per minute. When it answers
    /// 429 (rate limit), wait as long as it asks (capped at 60s) and try again, up to
    /// 2 more times, before giving up and marking the call Failed (which the manager can retry).
    /// </summary>
    private async Task<GroqEvaluationResult> EvaluateWithRateLimitRetryAsync(
        Guid callId, IReadOnlyList<SegmentInput> segments, IReadOnlyList<CriterionInput> criteria, CancellationToken ct)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await groq.EvaluateAsync(segments, criteria, modelOverride: null, ct);
            }
            catch (ProviderException ex) when (ex.IsRateLimited && attempt < maxAttempts)
            {
                var wait = ex.RetryAfter is { } retryAfter && retryAfter > TimeSpan.Zero
                    ? TimeSpan.FromSeconds(Math.Min(retryAfter.TotalSeconds + 1, 60))
                    : TimeSpan.FromSeconds(20);
                logger.LogWarning("Call {CallId}: Groq rate limit reached, waiting {Seconds}s before attempt {Attempt} of {Max}",
                    callId, (int)wait.TotalSeconds, attempt + 1, maxAttempts);
                await Task.Delay(wait, time, ct);
            }
        }
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private async Task SetStatusAsync(Call call, CallStatus status, CancellationToken ct)
    {
        call.Status = status;
        call.UpdatedAt = Now;
        await db.SaveChangesAsync(ct);
    }

    private async Task FailAsync(Call call, ProcessingStage stage, string message, CancellationToken ct)
    {
        db.ChangeTracker.Clear(); // drop anything half-added by the failed attempt
        var fresh = await db.Calls.FirstAsync(c => c.Id == call.Id, ct);
        fresh.Status = CallStatus.Failed;
        fresh.FailedStage = stage;
        fresh.ErrorMessage = message.Length <= 1000 ? message : message[..1000];
        fresh.UpdatedAt = Now;
        await db.SaveChangesAsync(ct);

        call.Status = CallStatus.Failed; // so ProcessAsync doesn't continue to the next stage
    }

    /// <summary>Readable text for the "Failed" card. Technical details stay in the server log.</summary>
    private static string FriendlyMessage(Exception ex, string service) => ex switch
    {
        ProviderException { IsRateLimited: true } =>
            $"The {service} service hit its rate limit. Retry in a minute.",
        ProviderException { StatusCode: 400 } when service == "transcription" =>
            "The transcription service couldn't read this recording. Check that it's a valid audio file.",
        ProviderException { StatusCode: 401 or 403 } =>
            $"The {service} service rejected the request (API key or network access). Check the server configuration.",
        ProviderException p =>
            $"The {service} service returned an error (HTTP {p.StatusCode}). Retry later.",
        JsonException =>
            "The AI returned an answer that couldn't be read. Retry the evaluation.",
        HttpRequestException or TaskCanceledException =>
            $"The {service} service couldn't be reached or took too long. Retry later.",
        FileNotFoundException or DirectoryNotFoundException =>
            "The recording file is missing from storage.",
        _ => $"The {service} failed unexpectedly. Retry, and check the server log if it happens again.",
    };
}
