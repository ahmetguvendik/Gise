using Gise.Catalog.Api.Authentication;
using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Gise.Catalog.Application.Features.Commands.Venue.Response;
using Gise.Catalog.Application.Features.Handlers.Venue.CommandHandlers;
using Gise.Catalog.Application.Features.Handlers.Venue.QueryHandlers;
using Gise.Catalog.Application.Features.Queries.Venue.Request;
using Gise.Catalog.Application.Features.Queries.Venue.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Catalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VenuesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GetAllVenuesResponse>>> GetAll(
        [FromServices] GetAllVenuesQueryHandler handler,
        CancellationToken cancellationToken)
        => Ok(await handler.Handle(new GetAllVenuesQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetVenueByIdResponse>> GetById(
        Guid id,
        [FromServices] GetVenueByIdQueryHandler handler,
        CancellationToken cancellationToken)
        => Ok(await handler.Handle(new GetVenueByIdQuery { Id = id }, cancellationToken));

    [Authorize(Policy = Policies.VenueManager)]
    [HttpPost]
    public async Task<ActionResult<CreateVenueResponse>> Create(
        [FromBody] CreateVenueCommand command,
        [FromServices] CreateVenueCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
