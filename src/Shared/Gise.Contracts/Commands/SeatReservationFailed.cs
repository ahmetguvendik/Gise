namespace Gise.Contracts.Commands;

// Reservation → Order: koltuklar tutulamadı (dolu, etkinlik satışta değil…). Sipariş iptal olur.
public sealed class SeatReservationFailed
{
    public Guid OrderId { get; set; }

    // Merkezi hata kodu (ör. HOLD_SEATS_UNAVAILABLE) ve müşteriye gösterilecek açıklama.
    public string ErrorCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
