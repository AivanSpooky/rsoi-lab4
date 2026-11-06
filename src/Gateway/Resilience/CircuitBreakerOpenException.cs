namespace Gateway.Resilience;

public sealed class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string service)
        : base($"Circuit breaker for {service} is open")
    {
        Service = service;
    }

    public string Service { get; }
}
