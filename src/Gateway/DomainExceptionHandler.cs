using Gateway.Models;
using Gateway.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace Gateway;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, body) = Map(exception);
        if (status is null)
            return false;

        context.Response.StatusCode = status.Value;
        await context.Response.WriteAsJsonAsync(body, cancellationToken);
        return true;
    }

    private static (int? Status, object? Body) Map(Exception exception)
    {
        return exception switch
        {
            ServiceUnavailableException e => (StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse(e.Message)),
            EntityNotFoundException e => (StatusCodes.Status404NotFound,
                new ErrorResponse(e.Message)),
            CarNotAvailableException => (StatusCodes.Status400BadRequest,
                new ValidationErrorResponse("Car is not available",
                    [new ErrorDescription("carUid", "Car is already reserved")])),
            _ => (null, null)
        };
    }
}
