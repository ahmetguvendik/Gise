namespace Gise.Reservation.Domain.Enums;

// Kalıcı durum. Tutma (10 dk) burada değil Redis'te; süresi dolunca kendiliğinden kalkar (bkz. tasarım 5).
public enum SeatStatus
{
    Available,
    Sold
}
