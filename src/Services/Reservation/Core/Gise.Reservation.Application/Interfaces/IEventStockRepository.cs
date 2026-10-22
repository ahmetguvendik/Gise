using Gise.Reservation.Domain.Entities;

namespace Gise.Reservation.Application.Interfaces;

public interface IEventStockRepository
{
    // Koltuklarıyla birlikte, salt okunur.
    Task<EventStock?> GetWithSeatsAsync(Guid eventId, CancellationToken cancellationToken);
}
