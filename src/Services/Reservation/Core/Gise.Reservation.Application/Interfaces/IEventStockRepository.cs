using Gise.Reservation.Domain.Entities;

namespace Gise.Reservation.Application.Interfaces;

public interface IEventStockRepository
{
    // Koltuklar olmadan, salt okunur.
    Task<EventStock?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);

    // Etkinliğin istenen koltukları; etkinlikte olmayan ID'ler sonuçta yer almaz.
    Task<IReadOnlyList<EventSeat>> GetSeatsAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds, CancellationToken cancellationToken);

    // GetSeatsAsync gibi; değişiklikler izlenir.
    Task<IReadOnlyList<EventSeat>> GetSeatsForUpdateAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds, CancellationToken cancellationToken);

    // Koltuklarıyla birlikte, salt okunur.
    Task<EventStock?> GetWithSeatsAsync(Guid eventId, CancellationToken cancellationToken);

    // Etkinlik başına koltuk ve satılmamış koltuk sayısı; stoğu olmayan etkinlik sonuçta yer almaz.
    Task<IReadOnlyList<EventAvailability>> GetAvailabilityAsync(IReadOnlyCollection<Guid> eventIds, CancellationToken cancellationToken);
}

public sealed record EventAvailability(Guid EventId, int SeatCount, int AvailableCount);
