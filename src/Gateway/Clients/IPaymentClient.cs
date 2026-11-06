using Gateway.Models;

namespace Gateway.Clients;

public interface IPaymentClient
{
    Task<PaymentInfo?> GetPaymentAsync(Guid paymentUid, CancellationToken cancellationToken = default);

    Task<PaymentInfo?> CreatePaymentAsync(int price, CancellationToken cancellationToken = default);

    Task CancelPaymentAsync(Guid paymentUid, CancellationToken cancellationToken = default);
}
