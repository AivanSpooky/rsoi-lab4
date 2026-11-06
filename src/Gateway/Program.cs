using Gateway;
using Gateway.Clients;
using Gateway.Queueing;
using Gateway.Resilience;
using Gateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var circuitBreakerOptions = builder.Configuration
    .GetSection("Resilience:CircuitBreaker")
    .Get<CircuitBreakerOptions>() ?? new CircuitBreakerOptions();
var retryQueueOptions = builder.Configuration
    .GetSection("Resilience:RetryQueue")
    .Get<RetryQueueOptions>() ?? new RetryQueueOptions();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(new CircuitBreakerRegistry(circuitBreakerOptions, TimeProvider.System));
builder.Services.AddSingleton<RetryQueue>();
builder.Services.AddSingleton(retryQueueOptions);
builder.Services.AddSingleton<QueuedOperationRunner>();
builder.Services.AddHostedService<RetryWorker>();
builder.Services.AddScoped<IRentalService, RentalService>();

AddServiceClient<ICarsClient, CarsClient>(builder, "Services:Cars");
AddServiceClient<IRentalClient, RentalClient>(builder, "Services:Rental");
AddServiceClient<IPaymentClient, PaymentClient>(builder, "Services:Payment");

var app = builder.Build();

app.UseExceptionHandler(_ => { });
app.MapControllers();
app.MapGet("/manage/health", () => Results.Ok());

app.Run();

static void AddServiceClient<TInterface, TClient>(WebApplicationBuilder builder, string addressKey)
    where TInterface : class
    where TClient : class, TInterface
{
    var timeout = builder.Configuration.GetValue("Services:Timeout", TimeSpan.FromSeconds(5));
    builder.Services.AddHttpClient<TInterface, TClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration[addressKey]!);
        client.Timeout = timeout;
    });
}
