using Gise.Notification.Domain.Common;
using Gise.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gise.Notification.InboxWorker;

// Notification veritabanındaki inbox, bilet sahipleri ve gönderilen bildirimler tablolarını görür. Tabloları
// notification-api'nin migration'ları kurar; worker migration çalıştırmaz, aynı snake_case adlandırmayla bağlanır.
public sealed class InboxWorkerDbContext(DbContextOptions<InboxWorkerDbContext> options) : DbContext(options)
{
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<TicketHolder> TicketHolders => Set<TicketHolder>();
    public DbSet<SentNotification> SentNotifications => Set<SentNotification>();

    // Kolon eşlemesi notification-api'dekiyle aynı olmalı: bildirim tipi metin.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>().Property(m => m.Payload).HasColumnType("jsonb");
        modelBuilder.Entity<SentNotification>().Property(n => n.Type).HasConversion<string>();
    }

    // notification-api'deki gibi: zaman damgaları kayıt sırasında doldurulur.
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
