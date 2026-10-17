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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetEventByIdResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetEventByIdQuery { Id = id }, cancellationToken));

    [Authorize(Policy = Policies.Organizer)]
    [HttpPost]
    public async Task<ActionResult<CreateEventResponse>> Create([FromBody] CreateEventCommand command, CancellationToken cancellationToken)
    {
        command.OrganizerId = User.FindFirst("sub")?.Value
            ?? throw UnauthorizedException.MissingClaim("sub");

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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
}
