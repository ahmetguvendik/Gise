using Gise.Search.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Search.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.MessageId).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Topic).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.TraceParent).HasMaxLength(64);
        builder.Property(m => m.Error).HasMaxLength(2000);

        // Tekrar eden mesajın veritabanı güvencesi.
        builder.HasIndex(m => m.MessageId).IsUnique();
        builder.HasIndex(m => m.CreatedAt).HasFilter("processed_at IS NULL AND NOT is_dead_lettered");
    }
}
