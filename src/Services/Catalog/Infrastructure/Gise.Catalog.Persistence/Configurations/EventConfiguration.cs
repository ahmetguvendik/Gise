using Gise.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Catalog.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.OrganizerId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.CancellationReason).HasMaxLength(500);

        // Etkinliği olan mekân silinemez.
        builder.HasOne<Venue>().WithMany().HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.PriceCategories).WithOne().HasForeignKey(p => p.EventId).OnDelete(DeleteBehavior.Cascade);

        // Liste sorgusu "yayındaki, başlamamış etkinlikler, tarihe göre" — bu indeksle karşılanır.
        builder.HasIndex(e => new { e.Status, e.StartsAt });
    }
}
