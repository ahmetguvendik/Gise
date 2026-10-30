using Gise.Ticket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gise.Ticket.OutboxWorker;

// Yalnızca Ticket veritabanındaki outbox tablosunu görür. Tabloyu ticket-api'nin migration'ları kurar;
// worker migration çalıştırmaz, aynı snake_case adlandırmayla aynı tabloya bağlanır.
public sealed class OutboxWorkerDbContext(DbContextOptions<OutboxWorkerDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
}
