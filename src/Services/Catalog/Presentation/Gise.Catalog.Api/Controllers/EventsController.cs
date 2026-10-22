using Gise.Catalog.Api.Authentication;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Catalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GetAllEventsResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetAllEventsQuery(), cancellationToken));

    [Authorize(Policy = Policies.Organizer)]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<GetMyEventsResponse>>> GetMine(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMyEventsQuery
        {
            OrganizerId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub")
        }, cancellationToken));

    [Authorize(Policy = Policies.EventModerator)]
    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<GetPendingEventsResponse>>> GetPending(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetPendingEventsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetEventByIdResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetEventByIdQuery
        {
            Id = id,
            UserId = User.FindFirst("sub")?.Value,
            IsAdmin = User.IsInRole(Roles.Admin)
        }, cancellationToken));

    [Authorize(Policy = Policies.Organizer)]
    [HttpPost]
    public async Task<ActionResult<CreateEventResponse>> Create([FromBody] CreateEventCommand command, CancellationToken cancellationToken)
    {
        command.OrganizerId = User.FindFirst("sub")?.Value
            ?? throw UnauthorizedException.MissingClaim("sub");

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // Taslağı sahibi ve admin, yayındakini yalnızca admin değiştirir; mekân yalnızca taslakta değişir.
    [Authorize(Policy = Policies.Organizer)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateEventResponse>> Update(Guid id, [FromBody] UpdateEventCommand command, CancellationToken cancellationToken)
    {
        command.EventId = id;
        command.UserId = User.FindFirst("sub")?.Value
            ?? throw UnauthorizedException.MissingClaim("sub");
        command.IsAdmin = User.IsInRole(Roles.Admin);

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    // Yalnızca taslak; yayındaki etkinlik iptal edilir.
    [Authorize(Policy = Policies.Organizer)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteEventCommand
        {
            EventId = id,
            UserId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub"),
            IsAdmin = User.IsInRole(Roles.Admin)
        }, cancellationToken);

        return NoContent();
    }

    // Geri alınamaz; sebep zorunlu.
    [Authorize(Policy = Policies.EventModerator)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<CancelEventResponse>> Cancel(Guid id, [FromBody] CancelEventCommand command, CancellationToken cancellationToken)
    {
        command.EventId = id;
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    // Organizatör taslağı admin onayına gönderir.
    [Authorize(Policy = Policies.Organizer)]
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SubmitEventCommand
        {
            EventId = id,
            UserId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub"),
            IsAdmin = User.IsInRole(Roles.Admin)
        }, cancellationToken);

        return NoContent();
    }

    // Onay bekleyen etkinlik değiştirilebilmesi için taslağa döner.
    [Authorize(Policy = Policies.Organizer)]
    [HttpPost("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new WithdrawEventCommand
        {
            EventId = id,
            UserId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub"),
            IsAdmin = User.IsInRole(Roles.Admin)
        }, cancellationToken);

        return NoContent();
    }

    // Admin onay bekleyeni sebebiyle taslağa geri gönderir.
    [Authorize(Policy = Policies.EventModerator)]
    [HttpPost("{id:guid}/return")]
    public async Task<IActionResult> Return(Guid id, [FromBody] ReturnEventCommand command, CancellationToken cancellationToken)
    {
        command.EventId = id;
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    // Önce /api/media ile yüklenir; bilgi ve fiyatla aynı yetki kuralları.
    [Authorize(Policy = Policies.Organizer)]
    [HttpPut("{id:guid}/poster")]
    public async Task<ActionResult<SetEventPosterResponse>> SetPoster(Guid id, [FromBody] SetEventPosterCommand command, CancellationToken cancellationToken)
    {
        command.EventId = id;
        command.UserId = User.FindFirst("sub")?.Value
            ?? throw UnauthorizedException.MissingClaim("sub");
        command.IsAdmin = User.IsInRole(Roles.Admin);

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [Authorize(Policy = Policies.Organizer)]
    [HttpPut("{id:guid}/prices")]
    public async Task<ActionResult<SetEventPricesResponse>> SetPrices(Guid id, [FromBody] SetEventPricesCommand command, CancellationToken cancellationToken)
    {
        command.EventId = id;
        command.UserId = User.FindFirst("sub")?.Value
            ?? throw UnauthorizedException.MissingClaim("sub");
        command.IsAdmin = User.IsInRole(Roles.Admin);

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [Authorize(Policy = Policies.EventModerator)]
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<PublishEventResponse>> Publish(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new PublishEventCommand { EventId = id }, cancellationToken));
}
