namespace Gise.Contracts.Commands;

// Order → Reservation: ödeme alındı, siparişin tuttuğu koltukları sat. Cevap SeatsConfirmed ya da SeatConfirmationFailed.
public sealed class ConfirmSeats
{
    public Guid OrderId { get; set; }
}
