using CarsService.Models;
using Microsoft.EntityFrameworkCore;

namespace CarsService.Data;

public class CarsDbContext : DbContext
{
    public CarsDbContext(DbContextOptions<CarsDbContext> options) : base(options) { }

    public DbSet<Car> Cars => Set<Car>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Car>(entity =>
        {
            entity.ToTable("cars", t =>
                t.HasCheckConstraint("CK_cars_type", "type IN ('SEDAN', 'SUV', 'MINIVAN', 'ROADSTER')"));
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(c => c.CarUid).HasColumnName("car_uid").IsRequired();
            entity.HasIndex(c => c.CarUid).IsUnique();
            entity.Property(c => c.Brand).HasColumnName("brand").HasMaxLength(80).IsRequired();
            entity.Property(c => c.Model).HasColumnName("model").HasMaxLength(80).IsRequired();
            entity.Property(c => c.RegistrationNumber).HasColumnName("registration_number").HasMaxLength(20).IsRequired();
            entity.Property(c => c.Power).HasColumnName("power");
            entity.Property(c => c.Price).HasColumnName("price").IsRequired();
            entity.Property(c => c.Type).HasColumnName("type").HasMaxLength(20);
            entity.Property(c => c.Availability).HasColumnName("availability").IsRequired();
        });
    }
}
