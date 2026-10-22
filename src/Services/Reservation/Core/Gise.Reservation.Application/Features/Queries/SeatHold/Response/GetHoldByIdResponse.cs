using Gise.Reservation.Application.Features.Commands.SeatHold.Response;

namespace Gise.Reservation.Application.Features.Queries.SeatHold.Response;

public sealed class GetHoldByIdResponse
{
    public Guid HoldId { get; set; }
    public Guid EventId { get; set; }

    // Active, Expired (süresi doldu), Released, Confirmed.
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public decimal TotalPrice { get; set; }
    public List<HeldSeatResponse> Seats { get; set; } = [];
}
