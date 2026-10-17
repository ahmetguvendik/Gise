using Gise.Catalog.Api.Authentication;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;
using Gise.Catalog.Application.Features.Handlers.Event.QueryHandlers;
using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Catalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EventsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GetAllEventsResponse>>> GetAll(
        [FromServices] GetAllEventsQueryHandler handler,
        CancellationToken cancellationToken)
        => Ok(await handler.Handle(new GetAllEventsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetEventByIdResponse>> GetById(
        Guid id,
        [FromServices] GetEventByIdQueryHandler handler,
        CancellationToken cancellationToken)
        => Ok(await handler.Handle(new GetEventByIdQuery { Id = id }, cancellationToken));

    [Authorize(Policy = Policies.Organizer)]
    [HttpPost]
    public async Task<ActionResult<CreateEventResponse>> Create(
        [FromBody] CreateEventCommand command,
        [FromServices] CreateEventCommandHandler handler,
        CancellationToken cancellationToken)
    {
        command.OrganizerId = User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Token'da sub claim'i yok.");

        var result = await handler.Handle(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
