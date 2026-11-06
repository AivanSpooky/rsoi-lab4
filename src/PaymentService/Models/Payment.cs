namespace PaymentService.Models;

public enum PaymentStatus
{
    PAID,
    CANCELED
}

public class Payment
{
    public int Id { get; set; }
    public Guid PaymentUid { get; set; }
    public PaymentStatus Status { get; set; }
    public int Price { get; set; }
}
