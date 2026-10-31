using Gise.Contracts.Realtime;

namespace Gise.SeatMap.Application.Interfaces;

// Reservation'ın koltuk değişikliği yayınları (Redis). Her mesaj için onMessage çağrılır; abonelik kapanana kadar sürer.
public interface ISeatUpdateFeed
{
    Task SubscribeAsync(Func<SeatsChanged, Task> onMessage, CancellationToken cancellationToken);
}
