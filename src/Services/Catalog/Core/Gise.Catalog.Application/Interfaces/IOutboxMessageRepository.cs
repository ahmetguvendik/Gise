namespace Gise.Catalog.Application.Interfaces;

// Olay, iş verisiyle aynı SaveChanges'te kaydedilir; Kafka'ya gönderimi Infrastructure'daki OutboxProcessor yapar.
public interface IOutboxMessageRepository
{
    Task AddAsync(Domain.Entities.OutboxMessage message, CancellationToken cancellationToken);
}
