using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/v1/payment")]
public class PaymentController : ControllerBase
{
    private readonly PaymentDbContext _db;

    public PaymentController(PaymentDbContext db) => _db = db;

    [HttpGet("{paymentUid:guid}")]
    public async Task<ActionResult<PaymentResponse>> GetPayment(Guid paymentUid)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.PaymentUid == paymentUid);
        if (payment is null)
            return NotFound(new ErrorResponse($"Payment with uid {paymentUid} not found"));

        return Ok(ToResponse(payment));
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var payment = new Payment
        {
            PaymentUid = Guid.NewGuid(),
            Status = PaymentStatus.PAID,
            Price = request.Price
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(payment));
    }

    [HttpPost("{paymentUid:guid}/cancel")]
    public async Task<IActionResult> CancelPayment(Guid paymentUid)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.PaymentUid == paymentUid);
        if (payment is null)
            return NotFound(new ErrorResponse($"Payment with uid {paymentUid} not found"));

        payment.Status = PaymentStatus.CANCELED;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static PaymentResponse ToResponse(Payment payment) =>
        new(payment.PaymentUid, payment.Status.ToString(), payment.Price);
}
