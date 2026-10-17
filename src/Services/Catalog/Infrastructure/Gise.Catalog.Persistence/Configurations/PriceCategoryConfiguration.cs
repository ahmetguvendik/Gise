using Gise.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Catalog.Persistence.Configurations;

public sealed class PriceCategoryConfiguration : IEntityTypeConfiguration<PriceCategory>
{
    public void Configure(EntityTypeBuilder<PriceCategory> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Price).HasPrecision(10, 2);

        builder.HasOne<Event>().WithMany().HasForeignKey(p => p.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Section>().WithMany().HasForeignKey(p => p.SectionId).OnDelete(DeleteBehavior.Restrict);

        // Bir bölümün bir etkinlikte tek fiyatı olur.
        builder.HasIndex(p => new { p.EventId, p.SectionId }).IsUnique();
    }
}
