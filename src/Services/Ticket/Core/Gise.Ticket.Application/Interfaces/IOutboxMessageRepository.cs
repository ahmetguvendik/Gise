namespace Gise.Ticket.Application.Interfaces;

// Kafka'ya gidecek olay iş verisiyle aynı SaveChanges'te outbox'a yazılır; ticket-outbox-worker gönderir.
public interface IOutboxMessageRepository
{
    // Anahtar etkinliğin ID'si: aynı etkinliğin olayları sırayla gider.
    Task AddTicketEventAsync<TEvent>(Guid eventId, TEvent payload, CancellationToken cancellationToken) where TEvent : notnull;
}
