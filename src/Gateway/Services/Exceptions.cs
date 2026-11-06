namespace Gateway.Services;

public sealed class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string service, Exception? inner = null)
        : base($"{service} Service unavailable", inner)
    {
        Service = service;
    }

    public string Service { get; }
}

public sealed class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message) : base(message)
    {
    }
}

public sealed class CarNotAvailableException : Exception
{
    public CarNotAvailableException()
        : base("Car is not available")
    {
    }
}
