using Gise.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Notification.Persistence.Configurations;

public sealed class TicketHolderConfiguration : IEntityTypeConfiguration<TicketHolder>
{
    public void Configure(EntityTypeBuilder<TicketHolder> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.CustomerId).HasMaxLength(64).IsRequired();
        builder.Property(h => h.Email).HasMaxLength(320).IsRequired();
        builder.Property(h => h.Name).HasMaxLength(200).IsRequired();
        builder.Property(h => h.EventTitle).HasMaxLength(200).IsRequired();
        builder.Property(h => h.VenueName).HasMaxLength(200).IsRequired();
        builder.Property(h => h.City).HasMaxLength(100).IsRequired();

        // İptal ve tarih değişikliğinde etkinliğin bilet sahipleri.
        builder.HasIndex(h => h.EventId);
    }
}
