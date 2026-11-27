using CargoFlow.Configs.Entities;
using CargoFlow.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Data;

public class CargoFlowDbContext(DbContextOptions<CargoFlowDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
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
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ShipmentDocument> ShipmentDocuments => Set<ShipmentDocument>();
    public DbSet<ShipmentTrackingEvent> ShipmentTrackingEvents => Set<ShipmentTrackingEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
        modelBuilder.ApplyConfiguration(new ShipmentDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new ShipmentTrackingEventConfiguration());
    }
}
