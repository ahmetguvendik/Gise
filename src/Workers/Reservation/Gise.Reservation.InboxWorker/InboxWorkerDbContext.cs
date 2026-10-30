using Gise.Reservation.Domain.Common;
using Gise.Reservation.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gise.Reservation.InboxWorker;

// Reservation veritabanındaki inbox ve stok tablolarını görür. Tabloları reservation-api'nin migration'ları kurar;
// worker migration çalıştırmaz, aynı snake_case adlandırmayla aynı tablolara bağlanır.
public sealed class InboxWorkerDbContext(DbContextOptions<InboxWorkerDbContext> options) : DbContext(options)
{
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<EventStock> EventStocks => Set<EventStock>();
    public DbSet<EventSeat> EventSeats => Set<EventSeat>();
    public DbSet<SeatHold> SeatHolds => Set<SeatHold>();

    // Kolon eşlemesi reservation-api'dekiyle aynı olmalı: durumlar metin, sürüm PostgreSQL'in xmin'i.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>().Property(m => m.Payload).HasColumnType("jsonb");
        modelBuilder.Entity<EventStock>().HasMany(s => s.Seats).WithOne().HasForeignKey(s => s.EventId);
        modelBuilder.Entity<EventSeat>().Property(s => s.Status).HasConversion<string>();
        modelBuilder.Entity<EventSeat>().Property(s => s.Price).HasPrecision(10, 2);
        modelBuilder.Entity<EventSeat>().Property(s => s.Version).IsRowVersion();
        modelBuilder.Entity<SeatHold>().Property(h => h.Status).HasConversion<string>();
    }

    // reservation-api'deki gibi: zaman damgaları kayıt sırasında doldurulur.
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
