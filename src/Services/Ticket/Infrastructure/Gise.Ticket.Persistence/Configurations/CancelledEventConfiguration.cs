using Gise.Ticket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gise.Ticket.Persistence.Configurations;

public sealed class CancelledEventConfiguration : IEntityTypeConfiguration<CancelledEvent>
{
    public void Configure(EntityTypeBuilder<CancelledEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
    }
}
