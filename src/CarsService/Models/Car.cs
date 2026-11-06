namespace CarsService.Models;

public class Car
{
    public int Id { get; set; }
    public Guid CarUid { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public int? Power { get; set; }
    public int Price { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool Availability { get; set; }
}
