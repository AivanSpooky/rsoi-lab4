using System.Net;
using Gateway.Queueing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gateway.UnitTests;

public class QueuedOperationRunnerTests
{
    private sealed record FakeOperation : QueuedOperation
    {
        private readonly int _failures;
        private readonly Exception _error;

        public FakeOperation(int failuresBeforeSuccess, Exception? error = null)
            : base("FakeOperation")
        {
            _failures = failuresBeforeSuccess;
            _error = error ?? new HttpRequestException("service down");
        }

        public int Attempts { get; private set; }

        public override Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            Attempts++;
            if (Attempts <= _failures)
                throw _error;
            return Task.CompletedTask;
        }
    }

    private static QueuedOperationRunner CreateRunner(TimeSpan retryInterval)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var options = new RetryQueueOptions
        {
            RetryInterval = retryInterval,
            AttemptTimeout = TimeSpan.FromSeconds(5)
        };
        return new QueuedOperationRunner(services, NullLogger<QueuedOperationRunner>.Instance, options);
    }

    [Fact]
    public async Task RunAsync_OperationSucceedsImmediately_RunsOnce()
    {
        var runner = CreateRunner(TimeSpan.Zero);
        var operation = new FakeOperation(0);

        await runner.RunAsync(operation, CancellationToken.None);

        Assert.Equal(1, operation.Attempts);
    }

    [Fact]
    public async Task RunAsync_OperationFailsTransiently_RetriesUntilSuccess()
    {
        var runner = CreateRunner(TimeSpan.FromMilliseconds(1));
        var operation = new FakeOperation(3);

        await runner.RunAsync(operation, CancellationToken.None);

        Assert.Equal(4, operation.Attempts);
    }

    [Fact]
    public async Task RunAsync_OperationFailsPermanently_DoesNotRetry()
    {
        var runner = CreateRunner(TimeSpan.FromMilliseconds(1));
        var operation = new FakeOperation(int.MaxValue,
            new HttpRequestException("gone", null, HttpStatusCode.NotFound));

        await runner.RunAsync(operation, CancellationToken.None);

        Assert.Equal(1, operation.Attempts);
    }

    [Fact]
    public async Task RunAsync_Success_MarksOperationCompleted()
    {
        var runner = CreateRunner(TimeSpan.Zero);
        var operation = new FakeOperation(1);

        await runner.RunAsync(operation, CancellationToken.None);

        Assert.True(await operation.WaitForCompletionAsync(TimeSpan.FromSeconds(5)));
    }
}
