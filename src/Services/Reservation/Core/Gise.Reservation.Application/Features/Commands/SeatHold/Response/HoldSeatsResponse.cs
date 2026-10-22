namespace Gise.Reservation.Application.Features.Commands.SeatHold.Response;

public sealed class HoldSeatsResponse
{
    public Guid HoldId { get; set; }
    public Guid EventId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public decimal TotalPrice { get; set; }
    public List<HeldSeatResponse> Seats { get; set; } = [];
}

public sealed class HeldSeatResponse
{
    public Guid SeatId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public decimal Price { get; set; }
}
