using CarsService.Data;
using CarsService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarsService.Controllers;

[ApiController]
[Route("api/v1/cars")]
public class CarsController : ControllerBase
{
    private readonly CarsDbContext _db;

    public CarsController(CarsDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<CarResponse>>> GetCars(
        [FromQuery] int? page,
        [FromQuery] int? size,
        [FromQuery] bool showAll = false)
    {
        var query = _db.Cars.AsQueryable();
        if (!showAll)
            query = query.Where(c => c.Availability);

        var totalElements = await query.CountAsync();
        var pageNumber = page ?? 1;
        var pageSize = size ?? totalElements;

        var items = await query
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => ToResponse(c))
            .ToListAsync();

        return Ok(new PaginationResponse<CarResponse>(pageNumber, items.Count, totalElements, items));
    }

    [HttpGet("{carUid:guid}")]
    public async Task<ActionResult<CarResponse>> GetCar(Guid carUid)
    {
        var car = await _db.Cars.FirstOrDefaultAsync(c => c.CarUid == carUid);
        if (car is null)
            return NotFound(new ErrorResponse($"Car with uid {carUid} not found"));

        return Ok(ToResponse(car));
    }

    [HttpPatch("{carUid:guid}")]
    public async Task<ActionResult<CarResponse>> UpdateAvailability(Guid carUid, [FromBody] UpdateAvailabilityRequest request)
    {
        var car = await _db.Cars.FirstOrDefaultAsync(c => c.CarUid == carUid);
        if (car is null)
            return NotFound(new ErrorResponse($"Car with uid {carUid} not found"));

        car.Availability = request.Available;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(car));
    }

    private static CarResponse ToResponse(Car car) => new(
        car.CarUid,
        car.Brand,
        car.Model,
        car.RegistrationNumber,
        car.Power,
        car.Type,
        car.Price,
        car.Availability);
}
