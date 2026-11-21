using CargoFlow.Domain;
using CargoFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class WeightTypeConfiguration : IEntityTypeConfiguration<WeightType>
{
    public void Configure(EntityTypeBuilder<WeightType> builder)
    {
        var weightTypes = new List<WeightType>()
        {
            new() { Id = 1, Name = WeightTypeName.Kilogram},
            new() { Id = 2, Name = WeightTypeName.Ton }
        };
        builder.HasData(weightTypes);
        builder
            .Property(w => w.Name)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(w => w.Name).IsUnique();

    }
}
