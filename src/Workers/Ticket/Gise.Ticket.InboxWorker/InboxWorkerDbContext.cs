using Gise.Ticket.Domain.Common;
using Gise.Ticket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gise.Ticket.InboxWorker;

// Ticket veritabanındaki inbox, bilet ve iptal edilen etkinlik tablolarını görür. Tabloları ticket-api'nin migration'ları
// kurar; worker migration çalıştırmaz, aynı snake_case adlandırmayla aynı tablolara bağlanır.
public sealed class InboxWorkerDbContext(DbContextOptions<InboxWorkerDbContext> options) : DbContext(options)
{
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<Domain.Entities.Ticket> Tickets => Set<Domain.Entities.Ticket>();
    public DbSet<CancelledEvent> CancelledEvents => Set<CancelledEvent>();

    // Kolon eşlemesi ticket-api'dekiyle aynı olmalı: durumlar metin.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>().Property(m => m.Payload).HasColumnType("jsonb");
        modelBuilder.Entity<Domain.Entities.Ticket>().Property(t => t.Status).HasConversion<string>();
        modelBuilder.Entity<Domain.Entities.Ticket>().Property(t => t.Price).HasPrecision(10, 2);
    }

    // ticket-api'deki gibi: zaman damgaları kayıt sırasında doldurulur.
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
