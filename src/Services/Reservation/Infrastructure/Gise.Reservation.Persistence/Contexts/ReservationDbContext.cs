using Gise.Reservation.Domain.Common;
using Gise.Reservation.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Gise.Reservation.Persistence.Contexts;

public sealed class ReservationDbContext(DbContextOptions<ReservationDbContext> options) : DbContext(options)
{
    public DbSet<EventStock> EventStocks => Set<EventStock>();
    public DbSet<EventSeat> EventSeats => Set<EventSeat>();
    public DbSet<SeatHold> SeatHolds => Set<SeatHold>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReservationDbContext).Assembly);

        // RabbitMQ tarafı (saga komutları) MassTransit'in hazır inbox/outbox'ını kullanır (bkz. tasarım 2.2). MassTransit'in
        // tabloları "mt_" önekiyle durur; Kafka tarafındaki kendi inbox'ımızla (inbox_messages) karışmasın.
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
