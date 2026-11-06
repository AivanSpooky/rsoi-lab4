using System.Threading.Channels;

namespace Gateway.Queueing;

public sealed class RetryQueue
{
    private readonly Channel<QueuedOperation> _channel =
        Channel.CreateUnbounded<QueuedOperation>(new UnboundedChannelOptions { SingleReader = true });

    public int Count => _channel.Reader.Count;

    public void Enqueue(QueuedOperation operation)
    {
        _channel.Writer.TryWrite(operation);
    }

    public IAsyncEnumerable<QueuedOperation> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
