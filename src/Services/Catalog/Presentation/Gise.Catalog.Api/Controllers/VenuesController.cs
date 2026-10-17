using Gise.Catalog.Api.Authentication;
using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Gise.Catalog.Application.Features.Commands.Venue.Response;
using Gise.Catalog.Application.Features.Queries.Venue.Request;
using Gise.Catalog.Application.Features.Queries.Venue.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Catalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VenuesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VenuesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GetAllVenuesResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetAllVenuesQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetVenueByIdResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetVenueByIdQuery { Id = id }, cancellationToken));

    [Authorize(Policy = Policies.VenueManager)]
    [HttpPost]
    public async Task<ActionResult<CreateVenueResponse>> Create([FromBody] CreateVenueCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
