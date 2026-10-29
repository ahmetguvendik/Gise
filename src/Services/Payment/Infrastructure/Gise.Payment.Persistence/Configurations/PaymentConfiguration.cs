using Gise.Payment.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Payment.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Domain.Entities.Payment>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Amount).HasPrecision(10, 2);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CardLast4).HasMaxLength(4).IsFixedLength().IsRequired();
        builder.Property(p => p.FailureReason).HasMaxLength(200);

        // Bir siparişin parası en fazla bir kez çekilir; iade edilen de çekilmiş sayılır.
        builder.HasIndex(p => p.OrderId, "ix_payments_order_id_paid")
            .IsUnique()
            .HasFilter($"status IN ('{PaymentStatus.Succeeded}', '{PaymentStatus.Refunded}')")
            .HasDatabaseName("ix_payments_order_id_paid");
        // İade, siparişin bütün denemelerini okur.
        builder.HasIndex(p => p.OrderId);
    }
}
