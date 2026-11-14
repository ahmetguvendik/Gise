using Gise.Audit.Api.Authentication;
using Gise.Audit.Application.Common;
using Gise.Audit.Application.Features.Queries.Record.Request;
using Gise.Audit.Application.Features.Queries.Record.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Audit.Api.Controllers;

// Admin: denetim kayıtları (yetkilendirme, giriş ve iş işlemleri). Kayıtlar Elasticsearch'te, 365 gün saklanır.
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/audit/records")]
public sealed class AuditRecordsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditRecordsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Yeniden eskiye. action, outcome (success/failure/unknown), actorId, from ve to isteğe bağlı süzgeçler. page 1'den
    // başlar, pageSize en fazla 100 (varsayılan 20); en fazla ilk 10.000 kayda ulaşılır, ötesi için tarihle daraltılır.
    [HttpGet]
    public async Task<ActionResult<PagedResult<SearchAuditRecordsResponse>>> Search([FromQuery] SearchAuditRecordsQuery query, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));
}
