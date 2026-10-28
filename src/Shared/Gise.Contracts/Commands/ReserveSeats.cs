namespace Gise.Contracts.Commands;

// Order → Reservation: satın alma saga'sının ilk adımı, siparişin koltuklarını süreli tut.
// Cevap SeatsReserved ya da SeatReservationFailed olarak döner.
public sealed class ReserveSeats
{
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }

    // Keycloak'taki müşteri (sub): bir müşterinin aynı etkinlikte tek tutması olur.
    public string CustomerId { get; set; } = string.Empty;
    public List<Guid> SeatIds { get; set; } = [];
}
