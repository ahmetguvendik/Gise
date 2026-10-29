namespace Gise.Contracts.Commands;

// Reservation → Order: koltuklar satıldı.
public sealed class SeatsConfirmed
{
    public Guid OrderId { get; set; }
}
