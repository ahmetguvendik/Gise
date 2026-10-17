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

        builder.Property(e => e.Title).HasMaxLength(Event.TitleMaxLength).IsRequired();
        builder.Property(e => e.Venue).HasMaxLength(Event.VenueMaxLength).IsRequired();
        builder.Property(e => e.OrganizerId).HasMaxLength(64).IsRequired();

        // Liste sorgusu "başlamamış etkinlikler, tarihe göre" — bu indeksle karşılanır.
        builder.HasIndex(e => e.StartsAt);
    }
}
