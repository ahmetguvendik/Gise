using Gise.Reservation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Reservation.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Topic).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Key).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.TraceParent).HasMaxLength(64);
        builder.Property(m => m.Error).HasMaxLength(2000);

        // Worker yalnızca bekleyenleri tarar; işlenmiş satırlar indekse girmez.
        builder.HasIndex(m => m.CreatedAt).HasFilter("processed_at IS NULL AND NOT is_dead_lettered");
    }
}
