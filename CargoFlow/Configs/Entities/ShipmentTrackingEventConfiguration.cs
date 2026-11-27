using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class ShipmentTrackingEventConfiguration : IEntityTypeConfiguration<ShipmentTrackingEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentTrackingEvent> builder)
    {
        builder
            .HasOne(e => e.CreatedByUser)
            .WithMany(p => p.ShipmentTrackingEvents)
            .HasForeignKey(e => e.CreatedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.Shipment)
            .WithMany(s => s.ShipmentTrackingEvents)
            .HasForeignKey(e => e.ShipmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
