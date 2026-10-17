using Gise.Catalog.Domain.Constants;
using Gise.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Catalog.Persistence.Configurations;

public sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Row).HasMaxLength(SeatConstants.RowMaxLength).IsRequired();

        // Aynı bölümde aynı sıra ve numarada ikinci bir koltuk olamaz.
        builder.HasIndex(s => new { s.SectionId, s.Row, s.Number }).IsUnique();
    }
}
