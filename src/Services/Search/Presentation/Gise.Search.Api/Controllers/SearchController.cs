using Gise.Search.Application.Features.Queries.Event.Request;
using Gise.Search.Application.Features.Queries.Event.Response;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Search.Api.Controllers;

// Herkese açık: ana sayfadaki etkinlik listesi, arama ve öneriler.
[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // q, city, from, to, page, pageSize; hepsi isteğe bağlı. Boş arama yaklaşan etkinlikleri tarihe göre döndürür.
    [HttpGet("events")]
    public async Task<ActionResult<SearchEventsResponse>> SearchEvents([FromQuery] SearchEventsQuery query, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));

    [HttpGet("suggest")]
    public async Task<ActionResult<SuggestResponse>> Suggest([FromQuery] SuggestQuery query, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));
}
