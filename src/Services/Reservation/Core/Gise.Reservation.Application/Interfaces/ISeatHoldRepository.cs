using Gise.Reservation.Domain.Entities;

namespace Gise.Reservation.Application.Interfaces;

public interface ISeatHoldRepository
{
    Task AddAsync(SeatHold hold, CancellationToken cancellationToken);

    // Salt okunur.
    Task<SeatHold?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Değişiklikler izlenir.
    Task<SeatHold?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
}
