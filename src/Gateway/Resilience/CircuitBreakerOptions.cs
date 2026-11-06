namespace Gateway.Resilience;

public sealed class CircuitBreakerOptions
{
    public int FailureThreshold { get; init; } = 5;

    public TimeSpan OpenDuration { get; init; } = TimeSpan.FromSeconds(10);
}
