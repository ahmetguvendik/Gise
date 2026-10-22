using Gise.Reservation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Reservation.Persistence.Configurations;

public sealed class EventStockConfiguration : IEntityTypeConfiguration<EventStock>
{
    public void Configure(EntityTypeBuilder<EventStock> builder)
    {
        builder.HasKey(s => s.Id);
        // Catalog'daki etkinliğin ID'si; burada üretilmez.
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.HasMany(s => s.Seats).WithOne().HasForeignKey(s => s.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}
