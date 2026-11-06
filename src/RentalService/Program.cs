using Microsoft.EntityFrameworkCore;
using RentalService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<RentalDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RentalDbContext>();
    db.Database.EnsureCreated();
}

app.MapControllers();
app.MapGet("/manage/health", () => Results.Ok());

app.Run();
