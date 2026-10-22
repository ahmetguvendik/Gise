namespace Gise.Reservation.Application.Interfaces;

// Koltuk tutmanın hızlı katmanı (Redis). Kilitler süreyle yazılır ve süre dolunca kendiliğinden kalkar.
public interface ISeatLockService
{
    // Koltukların hepsi boşsa ve kullanıcının bu etkinlikte başka tutması yoksa hepsini tutar; ya hepsi ya hiçbiri.
    Task<SeatLockResult> TryLockAsync(Guid eventId, string userId, IReadOnlyCollection<Guid> seatIds, Guid holdId, TimeSpan duration);

    // Yalnızca bu tutmaya ait kilitleri kaldırır; süresi dolup başkası tutmuşsa ona dokunmaz.
    Task ReleaseAsync(Guid eventId, string userId, IReadOnlyCollection<Guid> seatIds, Guid holdId);

    Task<IReadOnlySet<Guid>> GetLockedSeatIdsAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds);
}

public enum SeatLockResult
{
    Locked,
    SeatTaken,
    UserAlreadyHolding
}
