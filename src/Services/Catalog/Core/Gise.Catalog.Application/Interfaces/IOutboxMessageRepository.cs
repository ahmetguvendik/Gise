namespace Gise.Catalog.Application.Interfaces;

// Olay, iş verisiyle aynı SaveChanges'te kaydedilir; Kafka'ya catalog-outbox-worker gönderir.
public interface IOutboxMessageRepository
{
    // catalog.events'e gidecek olay. Anahtar etkinlik ID'si: aynı etkinliğin olayları aynı bölüme düşer, sırası korunur.
    Task AddCatalogEventAsync<TEvent>(Guid eventId, TEvent payload, CancellationToken cancellationToken) where TEvent : notnull;
}
