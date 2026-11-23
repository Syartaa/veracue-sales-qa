using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;
using VeraCue.Api.DTOs.Calls;
using VeraCue.Api.Entities;
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
    /// Until sign-in exists (Phase 6), every call is uploaded by the single demo manager.
    /// Null if that account hasn't been created (DEMO_MANAGER_PASSWORD missing).
    /// </summary>
    public async Task<Guid?> GetUploaderIdAsync(CancellationToken ct) =>
        await db.Users.Where(u => u.Email == DemoUserSeeder.Email).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct);

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

    public Task<CallDetailDto?> GetAsync(Guid id, CancellationToken ct) =>
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
