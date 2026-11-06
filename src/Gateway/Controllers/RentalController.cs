using Gateway.Models;
using Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[Route("api/v1/rental")]
public class RentalController : ControllerBase
{
    private readonly IRentalService _rentals;

    public RentalController(IRentalService rentals) => _rentals = rentals;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RentalResponse>>> GetRentals(
        [FromHeader(Name = "X-User-Name")] string? username,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(username))
            return BadRequest(MissingUsernameError());

        return Ok(await _rentals.GetRentalsAsync(username, cancellationToken));
    }

    [HttpGet("{rentalUid:guid}")]
    public async Task<ActionResult<RentalResponse>> GetRental(
        Guid rentalUid,
        [FromHeader(Name = "X-User-Name")] string? username,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(username))
            return BadRequest(MissingUsernameError());

        var rental = await _rentals.GetRentalAsync(rentalUid, username, cancellationToken);
        if (rental is null)
            return NotFound(new ErrorResponse($"Rental with uid {rentalUid} not found"));

        return Ok(rental);
    }

    [HttpPost]
    public async Task<ActionResult<CreateRentalResponse>> CreateRental(
        [FromBody] CreateRentalRequest request,
        [FromHeader(Name = "X-User-Name")] string? username,
        CancellationToken cancellationToken)
    {
        var errors = ValidateRequest(request, username);
        if (errors.Count > 0)
            return BadRequest(new ValidationErrorResponse("Invalid request", errors));

        return Ok(await _rentals.CreateRentalAsync(request, username!, cancellationToken));
    }

    [HttpPost("{rentalUid:guid}/finish")]
    public async Task<IActionResult> FinishRental(
        Guid rentalUid,
        [FromHeader(Name = "X-User-Name")] string? username,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(username))
            return BadRequest(MissingUsernameError());

        await _rentals.FinishRentalAsync(rentalUid, username, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{rentalUid:guid}")]
    public async Task<IActionResult> CancelRental(
        Guid rentalUid,
        [FromHeader(Name = "X-User-Name")] string? username,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(username))
            return BadRequest(MissingUsernameError());

        await _rentals.CancelRentalAsync(rentalUid, username, cancellationToken);
        return NoContent();
    }

    private static ValidationErrorResponse MissingUsernameError()
    {
        return new ValidationErrorResponse("Missing X-User-Name header",
            [new ErrorDescription("X-User-Name", "Header is required")]);
    }

    private static List<ErrorDescription> ValidateRequest(CreateRentalRequest request, string? username)
    {
        var errors = new List<ErrorDescription>();
        if (string.IsNullOrEmpty(username))
            errors.Add(new ErrorDescription("X-User-Name", "Header is required"));
        if (request.CarUid == Guid.Empty)
            errors.Add(new ErrorDescription("carUid", "Must be a valid car UUID"));
        if (!DateOnly.TryParse(request.DateFrom, out var dateFrom))
            errors.Add(new ErrorDescription("dateFrom", "Must be a valid ISO 8601 date"));
        if (!DateOnly.TryParse(request.DateTo, out var dateTo))
            errors.Add(new ErrorDescription("dateTo", "Must be a valid ISO 8601 date"));
        if (errors.Count == 0 && dateTo <= dateFrom)
            errors.Add(new ErrorDescription("dateTo", "Must be after dateFrom"));
        return errors;
    }
}
