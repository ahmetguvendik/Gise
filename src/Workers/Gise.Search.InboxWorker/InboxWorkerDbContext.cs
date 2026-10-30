using Gise.Search.Domain.Common;
using Gise.Search.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gise.Search.InboxWorker;

// Search veritabanındaki inbox tablosunu görür. Tabloyu search-api'nin migration'ları kurar; worker migration
// çalıştırmaz, aynı snake_case adlandırmayla aynı tabloya bağlanır.
public sealed class InboxWorkerDbContext(DbContextOptions<InboxWorkerDbContext> options) : DbContext(options)
{
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<InboxMessage>().Property(m => m.Payload).HasColumnType("jsonb");

    // search-api'deki gibi: zaman damgaları kayıt sırasında doldurulur.
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
