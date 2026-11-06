namespace CarsService.Models;

public record CarResponse(
    Guid CarUid,
    string Brand,
    string Model,
    string RegistrationNumber,
    int? Power,
    string Type,
    int Price,
    bool Available);

public record PaginationResponse<T>(int Page, int PageSize, int TotalElements, IReadOnlyList<T> Items);

public record UpdateAvailabilityRequest(bool Available);

public record ErrorResponse(string Message);
