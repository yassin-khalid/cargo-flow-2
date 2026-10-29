using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.Property(v => v.InspectionId).HasMaxLength(20).IsRequired();
        builder.Property(v => v.InsuranceId).HasMaxLength(20).IsRequired();
        builder.HasIndex(v => v.InspectionId).IsUnique();
        builder.HasIndex(v => v.InsuranceId).IsUnique();
        builder.ToTable(v => v.HasCheckConstraint("Ck_Vehicle_MaxWeight_Min", "[MaxWeight] > 0")); 
        
        builder
            .HasOne(v => v.Company)
            .WithMany(c => c.Vehicles)
            .HasForeignKey(v => v.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(v => v.WeightType)
            .WithMany(w => w.Vehicles)
            .HasForeignKey(v => v.WeightTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}