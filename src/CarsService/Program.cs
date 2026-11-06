using CarsService.Data;
using CarsService.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<CarsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CarsDbContext>();
    db.Database.EnsureCreated();

    if (!db.Cars.Any())
    {
        db.Cars.Add(new Car
        {
            CarUid = Guid.Parse("109b42f3-198d-4c89-9276-a7520a7120ab"),
            Brand = "Mercedes Benz",
            Model = "GLA 250",
            RegistrationNumber = "ЛО777Х799",
            Power = 249,
            Type = "SEDAN",
            Price = 3500,
            Availability = true
        });
        db.SaveChanges();
    }
}

app.MapControllers();
app.MapGet("/manage/health", () => Results.Ok());

app.Run();
