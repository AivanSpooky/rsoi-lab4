using Gateway.Clients;
using Gateway.Models;
using Gateway.Resilience;
using Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[Route("api/v1/cars")]
public class CarsController : ControllerBase
{
    private readonly ICarsClient _cars;
    private readonly CircuitBreakerRegistry _circuits;

    public CarsController(ICarsClient cars, CircuitBreakerRegistry circuits)
    {
        _cars = cars;
        _circuits = circuits;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<CarResponse>>> GetCars(
        [FromQuery] int? page,
        [FromQuery] int? size,
        [FromQuery] bool showAll,
        CancellationToken cancellationToken)
    {
        var result = await _circuits.For(ServiceNames.Cars).ExecuteAsync(
            () => _cars.GetCarsAsync(page, size, showAll, cancellationToken),
            exception => throw new ServiceUnavailableException(ServiceNames.Cars, exception));
        return Ok(result);
    }
}
