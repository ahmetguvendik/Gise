using Gise.Reservation.Application.Features.Queries.SeatHold.Response;
using MediatR;

namespace Gise.Reservation.Application.Features.Queries.SeatHold.Request;

// Tutmanın durumu ve kalan süresi; yalnızca sahibi ve admin görür.
public sealed class GetHoldByIdQuery : IRequest<GetHoldByIdResponse>
{
    public Guid HoldId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
