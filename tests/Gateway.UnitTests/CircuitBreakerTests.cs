using Gateway.Resilience;

namespace Gateway.UnitTests;

public class CircuitBreakerTests
{
    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan interval) => _now += interval;
    }

    private static (CircuitBreaker breaker, MutableTimeProvider time) CreateBreaker(int threshold = 3)
    {
        var time = new MutableTimeProvider();
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = threshold,
            OpenDuration = TimeSpan.FromSeconds(30)
        };
        return (new CircuitBreaker("service", options, time), time);
    }

    [Fact]
    public async Task ExecuteAsync_ActionSucceeds_ReturnsResult()
    {
        var (breaker, _) = CreateBreaker();

        var result = await breaker.ExecuteAsync(() => Task.FromResult(42));

        Assert.Equal(42, result);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_FailuresBelowThreshold_CircuitStaysClosed()
    {
        var (breaker, _) = CreateBreaker(threshold: 3);
        var calls = 0;

        for (var i = 0; i < 2; i++)
        {
            await breaker.ExecuteAsync(() =>
                {
                    calls++;
                    return Task.FromException<int>(new HttpRequestException("down"));
                },
                _ => -1);
        }

        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ExecuteAsync_FailuresReachThreshold_CircuitOpensAndShortCircuits()
    {
        var (breaker, _) = CreateBreaker(threshold: 3);
        var calls = 0;

        Task<int> Action()
        {
            calls++;
            return Task.FromException<int>(new HttpRequestException("down"));
        }

        for (var i = 0; i < 3; i++)
            await breaker.ExecuteAsync<int>(Action, _ => -1);

        Assert.Equal(CircuitState.Open, breaker.State);

        var result = await breaker.ExecuteAsync<int>(Action, _ => -1);
        Assert.Equal(-1, result);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task ExecuteAsync_OpenCircuit_ThrowsOpenExceptionWithoutFallback()
    {
        var (breaker, _) = CreateBreaker(threshold: 1);
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            breaker.ExecuteAsync(() => Task.FromException<int>(new HttpRequestException("down"))));

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            breaker.ExecuteAsync(() => Task.FromResult(1)));
    }

    [Fact]
    public async Task ExecuteAsync_AfterOpenDuration_ProbeSuccessClosesCircuit()
    {
        var (breaker, time) = CreateBreaker(threshold: 1);
        await breaker.ExecuteAsync(() => Task.FromException<int>(new HttpRequestException("down")), _ => -1);
        Assert.Equal(CircuitState.Open, breaker.State);

        time.Advance(TimeSpan.FromSeconds(31));

        var result = await breaker.ExecuteAsync(() => Task.FromResult(7), _ => -1);

        Assert.Equal(7, result);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_ProbeFails_CircuitReopens()
    {
        var (breaker, time) = CreateBreaker(threshold: 1);
        await breaker.ExecuteAsync(() => Task.FromException<int>(new HttpRequestException("down")), _ => -1);
        time.Advance(TimeSpan.FromSeconds(31));

        var result = await breaker.ExecuteAsync(
            () => Task.FromException<int>(new HttpRequestException("still down")), _ => -1);

        Assert.Equal(-1, result);
        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_BeforeOpenDuration_StaysOpen()
    {
        var (breaker, time) = CreateBreaker(threshold: 1);
        await breaker.ExecuteAsync(() => Task.FromException<int>(new HttpRequestException("down")), _ => -1);
        time.Advance(TimeSpan.FromSeconds(10));

        var calls = 0;
        var result = await breaker.ExecuteAsync(() =>
            {
                calls++;
                return Task.FromResult(1);
            },
            _ => -1);

        Assert.Equal(-1, result);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ExecuteAsync_ClientError_DoesNotTripCircuit()
    {
        var (breaker, _) = CreateBreaker(threshold: 2);

        for (var i = 0; i < 3; i++)
        {
            await breaker.ExecuteAsync(
                () => Task.FromException<int>(
                    new HttpRequestException("not found", null, System.Net.HttpStatusCode.NotFound)),
                _ => -1);
        }

        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public async Task Registry_For_ReturnsSameBreakerPerService()
    {
        var registry = new CircuitBreakerRegistry(new CircuitBreakerOptions(), TimeProvider.System);

        var first = registry.For("payment");
        var second = registry.For("payment");
        var other = registry.For("cars");

        Assert.Same(first, second);
        Assert.NotSame(first, other);
    }
}
