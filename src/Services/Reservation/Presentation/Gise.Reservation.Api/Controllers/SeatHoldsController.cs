using Gise.Reservation.Api.Authentication;
using Gise.Reservation.Application.Features.Commands.SeatHold.Request;
using Gise.Reservation.Application.Features.Commands.SeatHold.Response;
using Gise.Reservation.Application.Features.Queries.SeatHold.Request;
using Gise.Reservation.Application.Features.Queries.SeatHold.Response;
using Gise.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Reservation.Api.Controllers;

// Giriş yapmış kullanıcı koltuk tutar. Order gelince tutmayı RabbitMQ'daki ReserveSeats komutu da aynı komutla yapacak.
[ApiController]
[Authorize]
[Route("api/reservation/holds")]
public sealed class SeatHoldsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SeatHoldsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<HoldSeatsResponse>> Hold([FromBody] HoldSeatsCommand command, CancellationToken cancellationToken)
    {
        command.UserId = User.FindFirst("sub")?.Value
            ?? throw UnauthorizedException.MissingClaim("sub");

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.HoldId }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetHoldByIdResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetHoldByIdQuery
        {
            HoldId = id,
            UserId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub"),
            IsAdmin = User.IsInRole(Roles.Admin)
        }, cancellationToken));

    // Süresi dolmadan bırakır; tekrar çağrılması zararsızdır.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Release(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ReleaseHoldCommand
        {
            HoldId = id,
            UserId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub"),
            IsAdmin = User.IsInRole(Roles.Admin)
        }, cancellationToken);

        return NoContent();
    }
}
