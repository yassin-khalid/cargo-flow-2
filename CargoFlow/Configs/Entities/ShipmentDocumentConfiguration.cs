using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class ShipmentDocumentConfiguration : IEntityTypeConfiguration<ShipmentDocument>
{
    public void Configure(EntityTypeBuilder<ShipmentDocument> builder)
    {
        builder
            .HasOne(d => d.Shipment)
            .WithMany(s => s.Documents)
            .HasForeignKey(d => d.ShipmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
