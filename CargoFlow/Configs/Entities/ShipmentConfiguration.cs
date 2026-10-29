using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {

        builder
            .Property(s => s.Weight)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.ToTable(s => s.HasCheckConstraint("Ck_Shipment_Weight_Min", "[Weight] > 0"));

        builder
            .HasOne(s => s.Customer)
            .WithMany(c => c.Shipments)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(s => s.Pickup)
            .WithMany(a => a.PickupShipments)
            .HasForeignKey(s => s.PickupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(s => s.Destination)
            .WithMany(a => a.DestinationShipments)
            .HasForeignKey(s => s.DestinationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(s => s.WeightType)
            .WithMany(w => w.Shipments)
            .HasForeignKey(s => s.WeightTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(s => s.ShipmentStatus)
            .WithMany(t => t.Shipments)
            .HasForeignKey(s => s.ShipmentStatusId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
