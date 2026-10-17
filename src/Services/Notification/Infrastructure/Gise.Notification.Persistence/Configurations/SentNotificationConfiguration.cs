using Gise.Notification.Domain.Constants;
using Gise.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Notification.Persistence.Configurations;

public sealed class SentNotificationConfiguration : IEntityTypeConfiguration<SentNotification>
{
    public void Configure(EntityTypeBuilder<SentNotification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.RecipientId).HasMaxLength(64).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(SentNotificationConstants.SubjectMaxLength).IsRequired();

        builder.HasIndex(n => new { n.ReferenceId, n.Type, n.RecipientId }).IsUnique();
    }
}
