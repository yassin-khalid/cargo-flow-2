using CargoFlow.Domain;
using CargoFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class OfferStateConfiguration : IEntityTypeConfiguration<OfferState>
{
    public void Configure(EntityTypeBuilder<OfferState> builder)
    {
        var offerStates = new List<OfferState>()
        {
            new() { Id = 1, Name = OfferStateName.Submitted},
            new() { Id = 2, Name =  OfferStateName.UnderReview },
            new() { Id = 3, Name = OfferStateName.Accepted },
            new() { Id = 4, Name = OfferStateName.Rejected },
            new() { Id = 5, Name = OfferStateName.Expired },
            new() { Id = 6, Name = OfferStateName.Withdrawn }
        };

        builder.HasData(offerStates);
        builder.HasIndex(o => o.Name).IsUnique();

        builder.Property(o => o.Name)
            .HasConversion<string>()
            .HasMaxLength(20).IsRequired();
    }
}