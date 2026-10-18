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

    [Authorize(Policy = Policies.VenueManager)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateVenueResponse>> Update(Guid id, [FromBody] UpdateVenueCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    // Bölüm düzeni yalnızca mekânda etkinlik yokken değişir (aksi 422).
    [Authorize(Policy = Policies.VenueManager)]
    [HttpPut("{id:guid}/layout")]
    public async Task<ActionResult<UpdateVenueLayoutResponse>> UpdateLayout(Guid id, [FromBody] UpdateVenueLayoutCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [Authorize(Policy = Policies.VenueManager)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteVenueCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
