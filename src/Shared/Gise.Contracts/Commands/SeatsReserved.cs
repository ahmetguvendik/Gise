namespace Gise.Contracts.Commands;

// Reservation → Order: koltuklar tutuldu. Fiyatlar tutma anındaki fiyatlardır; sipariş bunları kendine kaydeder.
public sealed class SeatsReserved
{
    public Guid OrderId { get; set; }

    // Tutmanın bitişi: ödeme bu zamana kadar gelmezse koltuklar bırakılır.
    public DateTime ExpiresAt { get; set; }
    public decimal TotalPrice { get; set; }
    public List<ReservedSeat> Seats { get; set; } = [];
}

public sealed class ReservedSeat
{
    public Guid SeatId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public decimal Price { get; set; }
}
