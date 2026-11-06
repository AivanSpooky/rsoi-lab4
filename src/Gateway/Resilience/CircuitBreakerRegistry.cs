using System.Collections.Concurrent;

namespace Gateway.Resilience;

public sealed class CircuitBreakerRegistry
{
    private readonly ConcurrentDictionary<string, CircuitBreaker> _breakers = new();
    private readonly CircuitBreakerOptions _options;
    private readonly TimeProvider _time;

    public CircuitBreakerRegistry(CircuitBreakerOptions options, TimeProvider time)
    {
        _options = options;
        _time = time;
    }

    public CircuitBreaker For(string service)
    {
        return _breakers.GetOrAdd(service, name => new CircuitBreaker(name, _options, _time));
    }
}
