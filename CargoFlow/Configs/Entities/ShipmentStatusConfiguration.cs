using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class ShipmentStatusConfiguration : IEntityTypeConfiguration<ShipmentStatus>
{
    public void Configure(EntityTypeBuilder<ShipmentStatus> builder)
    {
        var shipmentStatuses = new List<ShipmentStatus>()
        {
            new() { Id = 1, Name = "Draft" },
            new() { Id = 2, Name = "Open for Offers" },
            new() { Id = 3, Name = "Offers Received" },
            new() { Id = 4, Name = "Awarded" },
            new() { Id = 5, Name = "In Transit" },
            new() { Id = 6, Name = "Delivered" },
            new() { Id = 7, Name = "Completed" },
            new() { Id = 8, Name = "Cancelled" }
        };

        builder.HasData(shipmentStatuses);
        builder.HasIndex(s => s.Name).IsUnique();

        builder.Property(s => s.Name).HasMaxLength(20).IsRequired();
    }
}