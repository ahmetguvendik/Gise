using Gise.Reservation.Application.Features.Queries.EventSeat.Request;
using Gise.Reservation.Application.Features.Queries.EventSeat.Response;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Reservation.Api.Controllers;

// /api/events Catalog'un; Reservation'ın uçları gateway'de /api/reservation altında.
[ApiController]
[Route("api/reservation/events")]
public sealed class EventSeatsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventSeatsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Koltuk haritası herkese açık.
    [HttpGet("{eventId:guid}/seats")]
    public async Task<ActionResult<GetEventSeatsResponse>> GetSeats(Guid eventId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetEventSeatsQuery { EventId = eventId }, cancellationToken));
}
