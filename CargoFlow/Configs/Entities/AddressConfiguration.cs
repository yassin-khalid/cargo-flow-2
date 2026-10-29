using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder
        .Property(a => a.Latitude)
        .HasPrecision(9, 6);

        builder
            .Property(a => a.Longitude)
            .HasPrecision(9, 6);

        builder.ToTable(a => a.HasCheckConstraint("Ck_Address_Latitude_Range", "[Latitude] BETWEEN -90 AND 90"));
        builder.ToTable(a => a.HasCheckConstraint("Ck_Address_Longitude_Range", "[Longitude] BETWEEN -180 AND 180"));
    }
}
