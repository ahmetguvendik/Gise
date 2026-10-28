using Gise.Order.Domain.Common;
using Gise.Order.Domain.Entities;
using Gise.Order.Persistence.Configurations;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Gise.Order.Persistence.Contexts;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.Order> Orders => Set<Domain.Entities.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);

        // Satın alma saga'sının durumu ve MassTransit'in hazır inbox/outbox'ı (RabbitMQ tarafı, bkz. tasarım 2.2).
        // MassTransit'in tabloları "mt_" önekiyle durur; kendi tablolarımızla karışmasın.
        new OrderStateMap().Configure(modelBuilder);
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
