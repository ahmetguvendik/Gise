using Gise.Reservation.Domain.Entities;

namespace Gise.Reservation.Application.Interfaces;

public interface ISeatHoldRepository
{
    Task AddAsync(SeatHold hold, CancellationToken cancellationToken);

    // Salt okunur.
    Task<SeatHold?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    // Değişiklikler izlenir.
    Task<SeatHold?> GetByOrderIdForUpdateAsync(Guid orderId, CancellationToken cancellationToken);
}
