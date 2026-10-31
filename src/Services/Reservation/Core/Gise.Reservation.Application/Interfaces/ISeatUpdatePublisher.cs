using Gise.Contracts.Realtime;

namespace Gise.Reservation.Application.Interfaces;

// Koltukların durumu değişince koltuk haritasına (SeatMap) haber verir: Redis yayını, kalıcı değil (bkz. tasarım 3.11).
// Kayıttan sonra çağrılır; yayınlanamazsa loglanır, iş bozulmaz: tarayıcı haritayı yeniden yükleyince düzelir.
public interface ISeatUpdatePublisher
{
    Task PublishAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds, SeatState state);
}
