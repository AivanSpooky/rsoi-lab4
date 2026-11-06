using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;

namespace RentalService.Controllers;

[ApiController]
[Route("api/v1/rental")]
public class RentalController : ControllerBase
{
    private readonly RentalDbContext _db;

    public RentalController(RentalDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RentalResponse>>> GetRentals([FromQuery] string username)
    {
        var rentals = await _db.Rentals
            .Where(r => r.Username == username)
            .OrderBy(r => r.Id)
            .ToListAsync();

        return Ok(rentals.Select(ToResponse));
    }

    [HttpGet("{rentalUid:guid}")]
    public async Task<ActionResult<RentalResponse>> GetRental(Guid rentalUid, [FromQuery] string username)
    {
        var rental = await _db.Rentals.FirstOrDefaultAsync(r => r.RentalUid == rentalUid && r.Username == username);
        if (rental is null)
            return NotFound(new ErrorResponse($"Rental with uid {rentalUid} not found"));

        return Ok(ToResponse(rental));
    }

    [HttpPost]
    public async Task<ActionResult<RentalResponse>> CreateRental([FromBody] CreateRentalRequest request)
    {
        var rental = new Rental
        {
            RentalUid = Guid.NewGuid(),
            Username = request.Username,
            PaymentUid = request.PaymentUid,
            CarUid = request.CarUid,
            DateFrom = ParseDate(request.DateFrom),
            DateTo = ParseDate(request.DateTo),
            Status = RentalStatus.IN_PROGRESS
        };

        _db.Rentals.Add(rental);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(rental));
    }

    [HttpPost("{rentalUid:guid}/finish")]
    public async Task<IActionResult> FinishRental(Guid rentalUid, [FromQuery] string username)
    {
        var rental = await _db.Rentals.FirstOrDefaultAsync(r => r.RentalUid == rentalUid && r.Username == username);
        if (rental is null)
            return NotFound(new ErrorResponse($"Rental with uid {rentalUid} not found"));

        rental.Status = RentalStatus.FINISHED;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{rentalUid:guid}/cancel")]
    public async Task<IActionResult> CancelRental(Guid rentalUid, [FromQuery] string username)
    {
        var rental = await _db.Rentals.FirstOrDefaultAsync(r => r.RentalUid == rentalUid && r.Username == username);
        if (rental is null)
            return NotFound(new ErrorResponse($"Rental with uid {rentalUid} not found"));

        rental.Status = RentalStatus.CANCELED;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static DateTime ParseDate(string date) =>
        DateTime.SpecifyKind(DateTime.Parse(date), DateTimeKind.Utc);

    private static RentalResponse ToResponse(Rental rental) => new(
        rental.RentalUid,
        rental.Username,
        rental.PaymentUid,
        rental.CarUid,
        rental.DateFrom.ToString("yyyy-MM-dd"),
        rental.DateTo.ToString("yyyy-MM-dd"),
        rental.Status.ToString());
}
