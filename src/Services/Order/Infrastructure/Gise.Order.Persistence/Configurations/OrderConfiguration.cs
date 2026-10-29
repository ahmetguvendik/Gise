using Gise.Order.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Order.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Domain.Entities.Order>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.CustomerId).HasMaxLength(64).IsRequired();
        builder.Property(o => o.EventTitle).HasMaxLength(200).IsRequired();
        builder.Property(o => o.VenueName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.City).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.TotalAmount).HasPrecision(10, 2);
        builder.Property(o => o.FailureReason).HasMaxLength(200);

        builder.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);

        // "Siparişlerim" sorgusu için.
        builder.HasIndex(o => new { o.CustomerId, o.CreatedAt });
    }
}
