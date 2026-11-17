using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder
            .HasOne(p => p.User)
            .WithOne(u => u.UserProfile)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(p => p.UserId)
            .IsUnique();

        builder
            .HasDiscriminator<string>("ProfileType")
            .HasValue<Customer>("Customer")
            .HasValue<Company>("Company");
    }
}
