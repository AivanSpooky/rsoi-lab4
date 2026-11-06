using Gateway.Models;

namespace Gateway.Services;

public interface IRentalService
{
    Task<IReadOnlyList<RentalResponse>> GetRentalsAsync(string username, CancellationToken cancellationToken);

    Task<RentalResponse?> GetRentalAsync(Guid rentalUid, string username, CancellationToken cancellationToken);

    Task<CreateRentalResponse> CreateRentalAsync(CreateRentalRequest request, string username,
        CancellationToken cancellationToken);

    Task FinishRentalAsync(Guid rentalUid, string username, CancellationToken cancellationToken);

    Task CancelRentalAsync(Guid rentalUid, string username, CancellationToken cancellationToken);
}
