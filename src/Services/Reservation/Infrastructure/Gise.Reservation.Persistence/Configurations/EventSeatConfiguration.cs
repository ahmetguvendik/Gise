using Gise.Reservation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Reservation.Persistence.Configurations;

public sealed class EventSeatConfiguration : IEntityTypeConfiguration<EventSeat>
{
    public void Configure(EntityTypeBuilder<EventSeat> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.SectionName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Row).HasMaxLength(10).IsRequired();
        builder.Property(s => s.Price).HasPrecision(10, 2);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Version).IsRowVersion();

        builder.HasOne<SeatHold>().WithMany().HasForeignKey(s => s.HoldId).OnDelete(DeleteBehavior.SetNull);

        // Bir etkinlikte her koltuk tek satırdır; çift satışın veritabanı güvencesi.
        builder.HasIndex(s => new { s.EventId, s.SeatId }).IsUnique();
        builder.HasIndex(s => new { s.EventId, s.Status });
    }
}
