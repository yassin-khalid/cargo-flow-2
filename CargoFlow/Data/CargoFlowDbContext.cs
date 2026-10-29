using CargoFlow.Configs.Entities;
using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Data;

public class CargoFlowDbContext(DbContextOptions<CargoFlowDbContext> options) : DbContext(options)
{
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<OfferState> OfferStates => Set<OfferState>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentStatus> ShipmentStatuses => Set<ShipmentStatus>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<WeightType> WeightTypes => Set<WeightType>();
    public DbSet<ShipmentStatusHistory> ShipmentStatusHistories => Set<ShipmentStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AddressConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new DriverConfiguration());
        modelBuilder.ApplyConfiguration(new OfferConfiguration());
        modelBuilder.ApplyConfiguration(new OfferStateConfiguration());
        modelBuilder.ApplyConfiguration(new ShipmentConfiguration());
        modelBuilder.ApplyConfiguration(new ShipmentStatusConfiguration());
        modelBuilder.ApplyConfiguration(new ShipmentStatusHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new VehicleConfiguration());
        modelBuilder.ApplyConfiguration(new WeightTypeConfiguration());
    }
}
