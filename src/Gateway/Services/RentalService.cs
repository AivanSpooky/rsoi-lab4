using Gateway.Clients;
using Gateway.Models;
using Gateway.Queueing;
using Gateway.Resilience;

namespace Gateway.Services;

public sealed class RentalService : IRentalService
{
    private readonly ICarsClient _cars;
    private readonly IRentalClient _rental;
    private readonly IPaymentClient _payment;
    private readonly CircuitBreakerRegistry _circuits;
    private readonly RetryQueue _queue;
    private readonly RetryQueueOptions _queueOptions;

    public RentalService(
        ICarsClient cars,
        IRentalClient rental,
        IPaymentClient payment,
        CircuitBreakerRegistry circuits,
        RetryQueue queue,
        RetryQueueOptions queueOptions)
    {
        _cars = cars;
        _rental = rental;
        _payment = payment;
        _circuits = circuits;
        _queue = queue;
        _queueOptions = queueOptions;
    }

    public async Task<IReadOnlyList<RentalResponse>> GetRentalsAsync(string username,
        CancellationToken cancellationToken)
    {
        var rentals = await ExecuteCriticalAsync(ServiceNames.Rental,
            () => _rental.GetRentalsAsync(username, cancellationToken));
        return await Task.WhenAll(rentals.Select(r => ToResponseAsync(r, cancellationToken)));
    }

    public async Task<RentalResponse?> GetRentalAsync(Guid rentalUid, string username,
        CancellationToken cancellationToken)
    {
        var rental = await ExecuteCriticalAsync(ServiceNames.Rental,
            () => _rental.GetRentalAsync(rentalUid, username, cancellationToken));
        return rental is null ? null : await ToResponseAsync(rental, cancellationToken);
    }

    public async Task<CreateRentalResponse> CreateRentalAsync(CreateRentalRequest request, string username,
        CancellationToken cancellationToken)
    {
        var car = await ReserveCarAsync(request.CarUid, cancellationToken);
        var price = RentalPriceCalculator.Calculate(request.DateFrom, request.DateTo, car.Price);
        var payment = await CreatePaymentOrRollbackAsync(price, car.CarUid, cancellationToken);
        var rental = await CreateRecordOrRollbackAsync(request, username, car, payment, cancellationToken);

        return new CreateRentalResponse(rental.RentalUid, rental.Status, car.CarUid,
            rental.DateFrom, rental.DateTo, payment);
    }

    public async Task FinishRentalAsync(Guid rentalUid, string username, CancellationToken cancellationToken)
    {
        var rental = await GetRentalRecordOrThrowAsync(rentalUid, username, cancellationToken);
        await TryReleaseCarAsync(rental.CarUid, cancellationToken);
        await FinishRentalRecordAsync(rentalUid, username, cancellationToken);
    }

    public async Task CancelRentalAsync(Guid rentalUid, string username, CancellationToken cancellationToken)
    {
        var rental = await GetRentalRecordOrThrowAsync(rentalUid, username, cancellationToken);
        await TryReleaseCarAsync(rental.CarUid, cancellationToken);
        await CancelRentalRecordAsync(rentalUid, username, cancellationToken);
        await TryCancelPaymentAsync(rental.PaymentUid, cancellationToken);
    }

    private async Task<CarResponse> ReserveCarAsync(Guid carUid, CancellationToken cancellationToken)
    {
        var car = await ExecuteCriticalAsync(ServiceNames.Cars,
            () => _cars.GetCarAsync(carUid, cancellationToken));
        if (car is null)
            throw new EntityNotFoundException($"Car with uid {carUid} not found");
        if (!car.Available)
            throw new CarNotAvailableException();

        await ExecuteCriticalAsync(ServiceNames.Cars,
            () => _cars.SetAvailabilityAsync(carUid, false, cancellationToken));
        return car;
    }

    private async Task<PaymentInfo> CreatePaymentOrRollbackAsync(int price, Guid carUid,
        CancellationToken cancellationToken)
    {
        try
        {
            var payment = await ExecuteCriticalAsync(ServiceNames.Payment,
                () => _payment.CreatePaymentAsync(price, cancellationToken));
            return payment ?? throw new ServiceUnavailableException(ServiceNames.Payment);
        }
        catch
        {
            await TryReleaseCarAsync(carUid, cancellationToken);
            throw;
        }
    }

    private async Task<RentalRecord> CreateRecordOrRollbackAsync(CreateRentalRequest request, string username,
        CarResponse car, PaymentInfo payment, CancellationToken cancellationToken)
    {
        try
        {
            var record = new CreateRentalRecordRequest(username, payment.PaymentUid, car.CarUid,
                request.DateFrom, request.DateTo);
            var rental = await ExecuteCriticalAsync(ServiceNames.Rental,
                () => _rental.CreateRentalAsync(record, cancellationToken));
            return rental ?? throw new ServiceUnavailableException(ServiceNames.Rental);
        }
        catch
        {
            await TryCancelPaymentAsync(payment.PaymentUid, cancellationToken);
            await TryReleaseCarAsync(car.CarUid, cancellationToken);
            throw;
        }
    }

    private async Task<RentalRecord> GetRentalRecordOrThrowAsync(Guid rentalUid, string username,
        CancellationToken cancellationToken)
    {
        var rental = await ExecuteCriticalAsync(ServiceNames.Rental,
            () => _rental.GetRentalAsync(rentalUid, username, cancellationToken));
        return rental ?? throw new EntityNotFoundException($"Rental with uid {rentalUid} not found");
    }

    private async Task CancelRentalRecordAsync(Guid rentalUid, string username, CancellationToken cancellationToken)
    {
        try
        {
            await _circuits.For(ServiceNames.Rental)
                .ExecuteAsync(() => _rental.CancelRentalAsync(rentalUid, username, cancellationToken));
        }
        catch (Exception exception) when (!ErrorClassifier.IsTransient(exception))
        {
        }
        catch (Exception)
        {
            var operation = new CancelRentalOperation(rentalUid, username);
            _queue.Enqueue(operation);
            await operation.WaitForCompletionAsync(_queueOptions.CompletionWaitTimeout);
        }
    }

    private async Task FinishRentalRecordAsync(Guid rentalUid, string username, CancellationToken cancellationToken)
    {
        try
        {
            await _circuits.For(ServiceNames.Rental)
                .ExecuteAsync(() => _rental.FinishRentalAsync(rentalUid, username, cancellationToken));
        }
        catch (Exception exception) when (!ErrorClassifier.IsTransient(exception))
        {
        }
        catch (Exception)
        {
            var operation = new FinishRentalOperation(rentalUid, username);
            _queue.Enqueue(operation);
            await operation.WaitForCompletionAsync(_queueOptions.CompletionWaitTimeout);
        }
    }

    private async Task TryReleaseCarAsync(Guid carUid, CancellationToken cancellationToken)
    {
        try
        {
            await _circuits.For(ServiceNames.Cars)
                .ExecuteAsync(() => _cars.SetAvailabilityAsync(carUid, true, cancellationToken));
        }
        catch (Exception exception) when (!ErrorClassifier.IsTransient(exception))
        {
        }
        catch (Exception)
        {
            _queue.Enqueue(new SetCarAvailabilityOperation(carUid, true));
        }
    }

    private async Task TryCancelPaymentAsync(Guid paymentUid, CancellationToken cancellationToken)
    {
        try
        {
            await _circuits.For(ServiceNames.Payment)
                .ExecuteAsync(() => _payment.CancelPaymentAsync(paymentUid, cancellationToken));
        }
        catch (Exception exception) when (!ErrorClassifier.IsTransient(exception))
        {
        }
        catch (Exception)
        {
            _queue.Enqueue(new CancelPaymentOperation(paymentUid));
        }
    }

    private async Task<RentalResponse> ToResponseAsync(RentalRecord rental, CancellationToken cancellationToken)
    {
        var carTask = _circuits.For(ServiceNames.Cars).ExecuteAsync(
            async () => (object?)await GetCarInfoAsync(rental.CarUid, cancellationToken),
            _ => (object?)new CarReference(rental.CarUid));
        var paymentTask = _circuits.For(ServiceNames.Payment).ExecuteAsync(
            async () => (object?)await _payment.GetPaymentAsync(rental.PaymentUid, cancellationToken),
            _ => (object?)EmptyObject.Instance);
        await Task.WhenAll(carTask, paymentTask);

        return new RentalResponse(
            rental.RentalUid, rental.Status, rental.DateFrom, rental.DateTo,
            carTask.Result ?? new CarReference(rental.CarUid),
            paymentTask.Result ?? EmptyObject.Instance);
    }

    private async Task<CarInfo?> GetCarInfoAsync(Guid carUid, CancellationToken cancellationToken)
    {
        var car = await _cars.GetCarAsync(carUid, cancellationToken);
        return car is null ? null : new CarInfo(car.CarUid, car.Brand, car.Model, car.RegistrationNumber);
    }

    private Task<T> ExecuteCriticalAsync<T>(string service, Func<Task<T>> call)
    {
        return _circuits.For(service).ExecuteAsync(call,
            exception => throw new ServiceUnavailableException(service, exception));
    }

    private Task ExecuteCriticalAsync(string service, Func<Task> call)
    {
        return ExecuteCriticalAsync<object?>(service, async () =>
        {
            await call();
            return null;
        });
    }
}
