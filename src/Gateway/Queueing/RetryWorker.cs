namespace Gateway.Queueing;

public sealed class RetryWorker : BackgroundService
{
    private readonly RetryQueue _queue;
    private readonly QueuedOperationRunner _runner;
    private readonly ILogger<RetryWorker> _logger;

    public RetryWorker(RetryQueue queue, QueuedOperationRunner runner, ILogger<RetryWorker> logger)
    {
        _queue = queue;
        _runner = runner;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Retry worker started");

        await foreach (var operation in _queue.ReadAllAsync(stoppingToken))
            await _runner.RunAsync(operation, stoppingToken);
    }
}
