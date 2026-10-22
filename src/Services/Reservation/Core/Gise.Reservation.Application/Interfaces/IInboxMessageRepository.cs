using Gise.Reservation.Domain.Entities;

namespace Gise.Reservation.Application.Interfaces;

// Kafka tüketicisi gelen olayı buraya yazar; asıl işi reservation-inbox-worker yapar.
public interface IInboxMessageRepository
{
    Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken);
    Task AddAsync(InboxMessage message, CancellationToken cancellationToken);
}
