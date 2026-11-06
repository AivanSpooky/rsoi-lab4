namespace Gateway.Resilience;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public sealed class CircuitBreaker
{
    private readonly object _sync = new();
    private readonly CircuitBreakerOptions _options;
    private readonly TimeProvider _time;

    private int _consecutiveFailures;
    private DateTimeOffset _openedAtUtc;
    private bool _probeInFlight;

    public CircuitBreaker(string name, CircuitBreakerOptions? options = null, TimeProvider? time = null)
    {
        Name = name;
        _options = options ?? new CircuitBreakerOptions();
        _time = time ?? TimeProvider.System;
    }

    public string Name { get; }

    public CircuitState State { get; private set; } = CircuitState.Closed;

    public int ConsecutiveFailures => _consecutiveFailures;

    public Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        return ExecuteAsync(action, exception => throw exception);
    }

    public Task ExecuteAsync(Func<Task> action)
    {
        return ExecuteAsync(async () =>
        {
            await action();
            return true;
        });
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<Exception, T> fallback)
    {
        if (!TryAcquirePermission())
            return fallback(new CircuitBreakerOpenException(Name));

        try
        {
            var result = await action();
            OnSuccess();
            return result;
        }
        catch (Exception exception)
        {
            OnCompleted(exception);
            return fallback(exception);
        }
    }

    private bool TryAcquirePermission()
    {
        lock (_sync)
        {
            if (State == CircuitState.Closed)
                return true;

            if (State == CircuitState.Open)
            {
                if (_time.GetUtcNow() - _openedAtUtc < _options.OpenDuration)
                    return false;

                State = CircuitState.HalfOpen;
                _probeInFlight = false;
            }

            if (_probeInFlight)
                return false;

            _probeInFlight = true;
            return true;
        }
    }

    private void OnSuccess()
    {
        lock (_sync)
        {
            _consecutiveFailures = 0;
            _probeInFlight = false;
            State = CircuitState.Closed;
        }
    }

    private void OnCompleted(Exception exception)
    {
        if (!ErrorClassifier.IsTransient(exception))
        {
            OnSuccess();
            return;
        }

        lock (_sync)
        {
            _consecutiveFailures++;
            _probeInFlight = false;
            if (State == CircuitState.HalfOpen || _consecutiveFailures >= _options.FailureThreshold)
                TripOpen();
        }
    }

    private void TripOpen()
    {
        State = CircuitState.Open;
        _openedAtUtc = _time.GetUtcNow();
    }
}
