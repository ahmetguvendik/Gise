using Gise.Ticket.Domain.Entities;
using Gise.Ticket.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Ticket.Persistence.Configurations;

public sealed class TicketTransferConfiguration : IEntityTypeConfiguration<TicketTransfer>
{
    public void Configure(EntityTypeBuilder<TicketTransfer> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.FromCustomerId).HasMaxLength(64).IsRequired();
        builder.Property(t => t.FromEmail).HasMaxLength(254).IsRequired();
        builder.Property(t => t.FromName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.ToEmail).HasMaxLength(254).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.ToCustomerId).HasMaxLength(64);
        builder.Property(t => t.Version).IsRowVersion();

        // Bir biletin aynı anda tek bekleyen aktarımı olur.
        builder.HasIndex(t => t.TicketId)
            .IsUnique()
            .HasFilter($"status = '{TicketTransferStatus.Pending}'");

        // "Size gönderilen biletler" sorgusu için.
        builder.HasIndex(t => new { t.ToEmail, t.Status });
    }
}
