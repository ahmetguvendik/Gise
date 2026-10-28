using Gise.Reservation.Application.Features.Commands.SeatHold.Response;
using MediatR;

namespace Gise.Reservation.Application.Features.Commands.SeatHold.Request;

// Siparişin koltuklarını süreli tutar. Order saga'sının ReserveSeats komutuyla çağrılır (bkz. tasarım 4).
public sealed class HoldSeatsCommand : IRequest<HoldSeatsResponse>
{
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }
    public List<Guid> SeatIds { get; set; } = [];
    public string CustomerId { get; set; } = string.Empty;
}
