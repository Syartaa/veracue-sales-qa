using System.Threading.Channels;

namespace VeraCue.Api.Services.Calls;

/// <summary>
/// In-memory "to do" list of call ids for the background worker.
/// It only says *which* call to look at; *what* to do next is decided from the call's
/// status in the database. So if the server restarts and this list is lost, nothing
/// is lost: the worker rebuilds it from the database on startup.
/// </summary>
public sealed class CallProcessingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid callId) => _channel.Writer.TryWrite(callId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
