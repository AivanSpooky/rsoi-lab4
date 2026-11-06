namespace Gateway.Queueing;

public abstract record QueuedOperation(string Name)
{
    private readonly TaskCompletionSource _completed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<bool> WaitForCompletionAsync(TimeSpan timeout)
    {
        var winner = await Task.WhenAny(_completed.Task, Task.Delay(timeout));
        return winner == _completed.Task;
    }

    internal void MarkCompleted() => _completed.TrySetResult();

    public abstract Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken);
}
