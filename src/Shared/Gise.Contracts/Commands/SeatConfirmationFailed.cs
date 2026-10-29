namespace Gise.Contracts.Commands;

// Reservation → Order: koltuklar satılamadı (tutmanın süresi dolup koltuk başka bir siparişe satılmış…).
// Sipariş iptal olur, ödeme iade edilir.
public sealed class SeatConfirmationFailed
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
