namespace Gateway.Queueing;

public sealed class RetryQueueOptions
{
    public TimeSpan RetryInterval { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan CompletionWaitTimeout { get; init; } = TimeSpan.FromSeconds(10);
}
