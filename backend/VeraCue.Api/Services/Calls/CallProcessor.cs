using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;
using VeraCue.Api.Entities;
using VeraCue.Api.Services.Storage;
using VeraCue.Api.Services.Transcription;
using SegmentEntity = VeraCue.Api.Entities.TranscriptSegment;

namespace VeraCue.Api.Services.Calls;

/// <summary>
/// Moves ONE call forward from its current status. Every stage saves its result before the
/// next starts, so a failure never means starting over:
///   Uploaded -> Transcribing -> Transcribed   (this phase)
///   Transcribed -> Evaluating -> ReadyForReview (Phase 4)
/// </summary>
public sealed class CallProcessor(
    AppDbContext db,
    AudioStorage storage,
    DeepgramTranscriber deepgram,
    TimeProvider time,
    ILogger<CallProcessor> logger)
{
    public async Task ProcessAsync(Guid callId, CancellationToken ct)
    {
        var call = await db.Calls.FirstOrDefaultAsync(c => c.Id == callId, ct);
        if (call is null) return;

        if (call.Status == CallStatus.Uploaded)
            await TranscribeAsync(call, ct);

        // Phase 4: if (call.Status == CallStatus.Transcribed) await EvaluateAsync(call, ct);
    }

    private async Task TranscribeAsync(Call call, CancellationToken ct)
    {
        // 1. Mark the stage as started, so the Processing page shows "Transcribing".
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
                await FailAsync(call, "No speech was detected in the recording.", ct);
                return;
            }

            // 2. Save the transcript and the new status together (one transaction).
            //    Old segments are removed first, in case an earlier attempt half-finished.
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
            call.UpdatedAt = time.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Call {CallId} transcribed: {Count} segments", call.Id, result.Segments.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Server is shutting down. The call stays "Transcribing" and the worker
            // picks it up again on the next start (see CallProcessingWorker).
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transcription failed for call {CallId}", call.Id);
            await FailAsync(call, FriendlyMessage(ex), ct);
        }
    }

    private async Task SetStatusAsync(Call call, CallStatus status, CancellationToken ct)
    {
        call.Status = status;
        call.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
    }

    private async Task FailAsync(Call call, string message, CancellationToken ct)
    {
        db.ChangeTracker.Clear(); // drop any half-added segments from the failed attempt
        var fresh = await db.Calls.FirstAsync(c => c.Id == call.Id, ct);
        fresh.Status = CallStatus.Failed;
        fresh.FailedStage = ProcessingStage.Transcription;
        fresh.ErrorMessage = message.Length <= 1000 ? message : message[..1000];
        fresh.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Readable text for the "Failed" card. Technical details stay in the server log.</summary>
    private static string FriendlyMessage(Exception ex) => ex switch
    {
        ProviderException { IsRateLimited: true } =>
            "The transcription service is busy (rate limit reached). Retry in a minute.",
        ProviderException { StatusCode: 400 } =>
            "The transcription service couldn't read this recording. Check that it's a valid audio file.",
        ProviderException { StatusCode: 401 or 403 } =>
            "The transcription service rejected the API key. Check the server configuration.",
        ProviderException p =>
            $"The transcription service returned an error (HTTP {p.StatusCode}). Retry later.",
        HttpRequestException or TaskCanceledException =>
            "The transcription service couldn't be reached or took too long. Retry later.",
        FileNotFoundException or DirectoryNotFoundException =>
            "The recording file is missing from storage.",
        _ => "Transcription failed unexpectedly. Retry, and check the server log if it happens again.",
    };
}
