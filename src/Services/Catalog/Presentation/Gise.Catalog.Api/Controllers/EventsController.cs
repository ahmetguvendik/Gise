using Gise.Catalog.Api.Authentication;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Mediator;
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
            ?? throw new InvalidOperationException("Token'da sub claim'i yok.");

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
