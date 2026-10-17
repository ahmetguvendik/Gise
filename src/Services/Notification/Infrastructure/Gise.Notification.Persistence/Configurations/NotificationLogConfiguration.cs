using Gise.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Notification.Persistence.Configurations;

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.RecipientId).HasMaxLength(64).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(NotificationLog.SubjectMaxLength).IsRequired();

        builder.HasIndex(n => new { n.ReferenceId, n.Type, n.RecipientId }).IsUnique();
    }
}
