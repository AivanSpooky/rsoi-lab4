using Gateway.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Queueing;

public sealed record CancelPaymentOperation(Guid PaymentUid)
    : QueuedOperation("CancelPayment")
{
    public override Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        return services.GetRequiredService<IPaymentClient>()
            .CancelPaymentAsync(PaymentUid, cancellationToken);
    }
}

public sealed record SetCarAvailabilityOperation(Guid CarUid, bool Available)
    : QueuedOperation("SetCarAvailability")
{
    public override Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        return services.GetRequiredService<ICarsClient>()
            .SetAvailabilityAsync(CarUid, Available, cancellationToken);
    }
}

public sealed record CancelRentalOperation(Guid RentalUid, string Username)
    : QueuedOperation("CancelRental")
{
    public override Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        return services.GetRequiredService<IRentalClient>()
            .CancelRentalAsync(RentalUid, Username, cancellationToken);
    }
}

public sealed record FinishRentalOperation(Guid RentalUid, string Username)
    : QueuedOperation("FinishRental")
{
    public override Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        return services.GetRequiredService<IRentalClient>()
            .FinishRentalAsync(RentalUid, Username, cancellationToken);
    }
}
