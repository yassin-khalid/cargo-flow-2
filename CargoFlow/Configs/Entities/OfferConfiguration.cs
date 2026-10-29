using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {

        builder
            .HasIndex((o => new { o.ShipmentId, o.CompanyId }))
            .IsUnique();
        
        builder
        .Property(o => o.HasWon)
        .HasDefaultValue(false)
        .IsRequired();

        builder
        .HasIndex(o => o.ShipmentId)
        .IsUnique()
        .HasFilter("[HasWon] = 1");

        builder
            .Property(o => o.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder
            .ToTable(o => o.HasCheckConstraint("Ck_Offer_Price_Min", "[Price] > 0"));

        builder
            .HasOne(o => o.Shipment)
            .WithMany(s => s.Offers)
            .HasForeignKey(o => o.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(o => o.Company)
            .WithMany(c => c.Offers)
            .HasForeignKey(o => o.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(o => o.OfferState)
            .WithMany(s => s.Offers)
            .HasForeignKey(o => o.OfferStateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
