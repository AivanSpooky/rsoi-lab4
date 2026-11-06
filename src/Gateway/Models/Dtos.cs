namespace Gateway.Models;

// Cars Service
public record CarResponse(
    Guid CarUid,
    string Brand,
    string Model,
    string RegistrationNumber,
    int? Power,
    string Type,
    int Price,
    bool Available);

public record CarInfo(Guid CarUid, string Brand, string Model, string RegistrationNumber);

public record CarReference(Guid CarUid);

public sealed record EmptyObject
{
    public static readonly EmptyObject Instance = new();

    private EmptyObject()
    {
    }
}

public record PaginationResponse<T>(int Page, int PageSize, int TotalElements, IReadOnlyList<T> Items);

public record UpdateAvailabilityRequest(bool Available);

// Rental Service
public record RentalRecord(
    Guid RentalUid,
    string Username,
    Guid PaymentUid,
    Guid CarUid,
    string DateFrom,
    string DateTo,
    string Status);

public record CreateRentalRecordRequest(
    string Username,
    Guid PaymentUid,
    Guid CarUid,
    string DateFrom,
    string DateTo);

// Payment Service
public record PaymentInfo(Guid PaymentUid, string Status, int Price);

public record CreatePaymentRequest(int Price);

// Gateway API
public record RentalResponse(
    Guid RentalUid,
    string Status,
    string DateFrom,
    string DateTo,
    object Car,
    object Payment);

public record CreateRentalRequest(Guid CarUid, string DateFrom, string DateTo);

public record CreateRentalResponse(
    Guid RentalUid,
    string Status,
    Guid CarUid,
    string DateFrom,
    string DateTo,
    PaymentInfo Payment);

public record ErrorResponse(string Message);

public record ErrorDescription(string Field, string Error);

public record ValidationErrorResponse(string Message, IReadOnlyList<ErrorDescription> Errors);
