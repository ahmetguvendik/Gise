using Gise.Reservation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Reservation.Persistence.Configurations;

public sealed class SeatHoldConfiguration : IEntityTypeConfiguration<SeatHold>
{
    public void Configure(EntityTypeBuilder<SeatHold> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.UserId).HasMaxLength(64).IsRequired();
        builder.Property(h => h.Status).HasConversion<string>().HasMaxLength(20);

        // Aynı sipariş için ikinci bir tutma oluşmaz; tekrar gelen ReserveSeats komutu zararsız olur.
        builder.HasIndex(h => h.OrderId).IsUnique();

        builder.HasIndex(h => new { h.EventId, h.UserId });
    }
}
