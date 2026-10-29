using Gise.Payment.Domain.Common;
using Gise.Payment.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Gise.Payment.Persistence.Contexts;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.Payment> Payments => Set<Domain.Entities.Payment>();
    public DbSet<OrderPayment> OrderPayments => Set<OrderPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);

        // Saga komutları MassTransit'in hazır inbox/outbox'ını kullanır (bkz. tasarım 2.2); tabloları "mt_" önekli.
        modelBuilder.AddInboxStateEntity(e => e.ToTable("mt_inbox_state"));
        modelBuilder.AddOutboxMessageEntity(e => e.ToTable("mt_outbox_message"));
        modelBuilder.AddOutboxStateEntity(e => e.ToTable("mt_outbox_state"));
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Property(e => e.CreatedAt).CurrentValue = now;
            else if (entry.State == EntityState.Modified)
                entry.Property(e => e.UpdatedAt).CurrentValue = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
