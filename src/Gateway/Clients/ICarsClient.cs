using Gateway.Models;

namespace Gateway.Clients;

public interface ICarsClient
{
    Task<PaginationResponse<CarResponse>?> GetCarsAsync(int? page, int? size, bool showAll,
        CancellationToken cancellationToken = default);

    Task<CarResponse?> GetCarAsync(Guid carUid, CancellationToken cancellationToken = default);

    Task SetAvailabilityAsync(Guid carUid, bool available, CancellationToken cancellationToken = default);
}
