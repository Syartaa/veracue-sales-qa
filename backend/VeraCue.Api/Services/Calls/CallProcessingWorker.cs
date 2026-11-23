using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Services.Calls;

/// <summary>
/// Background worker inside the API process. Takes call ids from the queue and processes
/// them ONE AT A TIME (keeps memory low on the small server and stays within AI rate limits).
/// On startup it re-queues calls that were mid-processing when the server last stopped.
/// </summary>
public sealed class CallProcessingWorker(
    CallProcessingQueue queue,
    IServiceScopeFactory scopes,
    ILogger<CallProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverUnfinishedCallsAsync(stoppingToken);

        await foreach (var callId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                // A fresh scope per call = a fresh DbContext, like a separate HTTP request.
                using var scope = scopes.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<CallProcessor>();
                await processor.ProcessAsync(callId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // shutting down; unfinished calls are recovered on the next start
            }
            catch (Exception ex)
            {
                // Never let one bad call stop the worker for all the others.
                logger.LogError(ex, "Unexpected error while processing call {CallId}", callId);
            }
        }
    }

    /// <summary>
    /// A call left "Transcribing" means the server stopped mid-way: put it back to "Uploaded".
    /// Then queue every call that is waiting for work, oldest first.
    /// </summary>
    private async Task RecoverUnfinishedCallsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var interrupted = await db.Calls
                .Where(c => c.Status == CallStatus.Transcribing)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Status, CallStatus.Uploaded)
                    .SetProperty(c => c.UpdatedAt, DateTime.UtcNow), ct);

            var waiting = await db.Calls
                .Where(c => c.Status == CallStatus.Uploaded)
                .OrderBy(c => c.UploadedAt)
                .Select(c => c.Id)
                .ToListAsync(ct);

            waiting.ForEach(queue.Enqueue);

            if (interrupted > 0 || waiting.Count > 0)
                logger.LogInformation("Recovered {Interrupted} interrupted call(s); queued {Waiting} call(s).",
                    interrupted, waiting.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not recover unfinished calls on startup.");
        }
    }
}
