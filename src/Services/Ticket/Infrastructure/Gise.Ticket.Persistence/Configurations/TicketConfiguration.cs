using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Ticket.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Domain.Entities.Ticket>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Ticket> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.CustomerId).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Code).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.PdfKey).HasMaxLength(300);
        builder.Property(t => t.CheckedInBy).HasMaxLength(64);

        builder.HasIndex(t => t.Code).IsUnique();

        // Aynı etkinlikte aynı koltuğa iptal edilmemiş ikinci bir bilet çıkamaz.
        builder.HasIndex(t => new { t.EventId, t.SeatId }).IsUnique().HasFilter("status <> 'Cancelled'");

        builder.HasIndex(t => t.OrderId);
        builder.HasIndex(t => t.CustomerId);
    }
}
