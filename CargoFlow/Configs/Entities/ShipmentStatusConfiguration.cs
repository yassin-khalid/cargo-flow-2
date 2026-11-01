using CargoFlow.Domain;
using CargoFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class ShipmentStatusConfiguration : IEntityTypeConfiguration<ShipmentStatus>
{
    public void Configure(EntityTypeBuilder<ShipmentStatus> builder)
    {
        var shipmentStatuses = new List<ShipmentStatus>()
        {
            new() { Id = 1, Name = ShipmentStatusName.Draft },
            new() { Id = 2, Name = ShipmentStatusName.OpenForOffers},
            new() { Id = 3, Name = ShipmentStatusName.OffersReceived},
            new() { Id = 4, Name = ShipmentStatusName.Awarded },
            new() { Id = 5, Name = ShipmentStatusName.InTransit},
            new() { Id = 6, Name = ShipmentStatusName.Delivered },
            new() { Id = 7, Name = ShipmentStatusName.Completed },
            new() { Id = 8, Name = ShipmentStatusName.Cancelled }
        };

        builder.HasData(shipmentStatuses);
        builder.HasIndex(s => s.Name).IsUnique();

        builder.Property(s => s.Name)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
    }
}