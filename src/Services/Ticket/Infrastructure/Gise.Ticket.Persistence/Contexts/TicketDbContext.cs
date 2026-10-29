using Gise.Ticket.Domain.Common;
using Gise.Ticket.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Gise.Ticket.Persistence.Contexts;

public sealed class TicketDbContext(DbContextOptions<TicketDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.Ticket> Tickets => Set<Domain.Entities.Ticket>();
    public DbSet<CancelledEvent> CancelledEvents => Set<CancelledEvent>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketDbContext).Assembly);

        // Saga komutları (IssueTickets) MassTransit'in hazır inbox/outbox'ını kullanır (bkz. tasarım 2.2). MassTransit'in
        // tabloları "mt_" önekiyle durur; Kafka tarafındaki kendi inbox ve outbox'ımızla (inbox_messages, outbox_messages)
        // karışmasın.
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
