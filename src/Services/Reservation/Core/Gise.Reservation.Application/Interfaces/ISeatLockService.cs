namespace Gise.Reservation.Application.Interfaces;

// Koltuk tutmanın hızlı katmanı (Redis). Kilitler süreyle yazılır ve süre dolunca kendiliğinden kalkar; kilidin değeri
// onu alan siparişin ID'sidir.
public interface ISeatLockService
{
    // Koltukların hepsi boşsa ve müşterinin bu etkinlikte başka siparişi koltuk tutmuyorsa hepsini tutar; ya hepsi ya hiçbiri.
    // Aynı siparişin kendi kilidi engel sayılmaz (komut yeniden gelebilir).
    Task<SeatLockResult> TryLockAsync(Guid eventId, string customerId, IReadOnlyCollection<Guid> seatIds, Guid orderId, TimeSpan duration);

    // Yalnızca bu siparişe ait kilitleri kaldırır; süre dolup başka bir sipariş tutmuşsa ona dokunmaz.
    Task ReleaseAsync(Guid eventId, string customerId, IReadOnlyCollection<Guid> seatIds, Guid orderId);

    Task<IReadOnlySet<Guid>> GetLockedSeatIdsAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds);
}

public enum SeatLockResult
{
    Locked,
    SeatTaken,
    UserAlreadyHolding
}
