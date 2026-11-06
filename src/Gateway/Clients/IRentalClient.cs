using Gateway.Models;

namespace Gateway.Clients;

public interface IRentalClient
{
    Task<List<RentalRecord>> GetRentalsAsync(string username, CancellationToken cancellationToken = default);

    Task<RentalRecord?> GetRentalAsync(Guid rentalUid, string username,
        CancellationToken cancellationToken = default);

    Task<RentalRecord?> CreateRentalAsync(CreateRentalRecordRequest request,
        CancellationToken cancellationToken = default);

    Task FinishRentalAsync(Guid rentalUid, string username, CancellationToken cancellationToken = default);

    Task CancelRentalAsync(Guid rentalUid, string username, CancellationToken cancellationToken = default);
}
