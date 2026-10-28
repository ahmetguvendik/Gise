using Gise.Order.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Order.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.SectionName).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Row).HasMaxLength(10).IsRequired();
        builder.Property(i => i.Price).HasPrecision(10, 2);

        builder.HasIndex(i => new { i.OrderId, i.SeatId }).IsUnique();
    }
}
