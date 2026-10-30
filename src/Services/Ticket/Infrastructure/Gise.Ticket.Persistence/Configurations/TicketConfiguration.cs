using Gise.Ticket.Domain.Enums;
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
        builder.Property(t => t.EventTitle).HasMaxLength(200).IsRequired();
        builder.Property(t => t.VenueName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.City).HasMaxLength(100).IsRequired();
        builder.Property(t => t.SectionName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Row).HasMaxLength(10).IsRequired();
        builder.Property(t => t.Price).HasPrecision(10, 2);
        builder.Property(t => t.Code).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.PdfKey).HasMaxLength(300).IsRequired();
        builder.Property(t => t.CheckedInBy).HasMaxLength(64);
        builder.Property(t => t.Version).IsRowVersion();

        builder.HasIndex(t => t.Code).IsUnique();

        // Aynı etkinlikte aynı koltuğa iptal edilmemiş ikinci bir bilet çıkamaz.
        builder.HasIndex(t => new { t.EventId, t.SeatId })
            .IsUnique()
            .HasFilter($"status <> '{TicketStatus.Cancelled}'");

        builder.HasIndex(t => t.OrderId);
        // "Biletlerim" sorgusu için.
        builder.HasIndex(t => new { t.CustomerId, t.EventStartsAt });
    }
}
