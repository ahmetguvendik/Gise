using Gise.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Catalog.Persistence.Configurations;

public sealed class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(Section.NameMaxLength).IsRequired();

        builder.HasMany(s => s.Seats).WithOne().HasForeignKey(seat => seat.SectionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Seats).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => new { s.VenueId, s.Name }).IsUnique();
    }
}
