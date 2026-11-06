using Gateway.Resilience;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Queueing;

public sealed class QueuedOperationRunner
{
    private readonly IServiceProvider _services;
    private readonly ILogger<QueuedOperationRunner> _logger;
    private readonly RetryQueueOptions _options;

    public QueuedOperationRunner(
        IServiceProvider services,
        ILogger<QueuedOperationRunner> logger,
        RetryQueueOptions options)
    {
        _services = services;
        _logger = logger;
        _options = options;
    }

    public async Task RunAsync(QueuedOperation operation, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var outcome = await TryRunOnceAsync(operation, cancellationToken);
            if (outcome != AttemptOutcome.Retry)
            {
                operation.MarkCompleted();
                return;
            }

            await Task.Delay(_options.RetryInterval, cancellationToken);
        }
    }

    private async Task<AttemptOutcome> TryRunOnceAsync(QueuedOperation operation, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_options.AttemptTimeout);

            await operation.ExecuteAsync(scope.ServiceProvider, timeout.Token);
            _logger.LogInformation("Queued operation {Operation} completed", operation.Name);
            return AttemptOutcome.Success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (!ErrorClassifier.IsTransient(exception))
        {
            _logger.LogWarning(exception,
                "Queued operation {Operation} failed permanently and was dropped", operation.Name);
            return AttemptOutcome.Abandon;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Queued operation {Operation} failed, retrying in {Interval}",
                operation.Name, _options.RetryInterval);
            return AttemptOutcome.Retry;
        }
    }

    private enum AttemptOutcome
    {
        Success,
        Retry,
        Abandon
    }
}
