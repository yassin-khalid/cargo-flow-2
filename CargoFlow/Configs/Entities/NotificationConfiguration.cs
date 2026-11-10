using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CargoFlow.Configs.Entities;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Message)
        .HasMaxLength(100)
        .IsRequired();

        builder.Property(n => n.Title)
        .HasMaxLength(25)
        .IsRequired();

        builder.Property(n => n.Type)
        .HasConversion<string>()
        .HasMaxLength(50)
        .IsRequired();

        builder.Property(n => n.RecipientType)
        .HasConversion<string>()
        .HasMaxLength(50)
        .IsRequired();
    }
}
