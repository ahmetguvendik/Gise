using Gise.Catalog.Domain.Constants;
using Gise.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Catalog.Persistence.Configurations;

public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Name).HasMaxLength(VenueConstants.NameMaxLength).IsRequired();
        builder.Property(v => v.City).HasMaxLength(VenueConstants.CityMaxLength).IsRequired();

        builder.HasMany(v => v.Sections).WithOne().HasForeignKey(s => s.VenueId).OnDelete(DeleteBehavior.Cascade);
    }
}
