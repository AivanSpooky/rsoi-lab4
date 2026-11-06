namespace PaymentService.Models;

public record PaymentResponse(Guid PaymentUid, string Status, int Price);

public record CreatePaymentRequest(int Price);

public record ErrorResponse(string Message);
