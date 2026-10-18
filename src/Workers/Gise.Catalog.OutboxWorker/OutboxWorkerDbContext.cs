using Gise.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gise.Catalog.OutboxWorker;

// Yalnızca Catalog veritabanındaki outbox tablosunu görür. Tabloyu catalog-api'nin migration'ları kurar;
// worker migration çalıştırmaz, aynı snake_case adlandırmayla aynı tabloya bağlanır.
public sealed class OutboxWorkerDbContext(DbContextOptions<OutboxWorkerDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
}
