using Gise.Exceptions;
using Gise.Authentication;
using Gise.Ticket.Api.Authentication;
using Gise.Ticket.Application.Features.Commands.Ticket.Request;
using Gise.Ticket.Application.Features.Commands.Ticket.Response;
using Gise.Ticket.Application.Features.Queries.Ticket.Request;
using Gise.Ticket.Application.Features.Queries.Ticket.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Ticket.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // orderId verilirse yalnızca o siparişin biletleri (sipariş sayfası).
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<GetMyTicketsResponse>>> GetMine([FromQuery] Guid? orderId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMyTicketsQuery
        {
            CustomerId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub"),
            OrderId = orderId
        }, cancellationToken));

    // PDF API'den geçmez: kısa süreli imzalı adres döner, tarayıcı dosyayı MinIO'dan indirir.
    [HttpGet("{id:guid}/download")]
    public async Task<ActionResult<GetTicketDownloadUrlResponse>> GetDownloadUrl(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetTicketDownloadUrlQuery
        {
            TicketId = id,
            CustomerId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub"),
            IsAdmin = User.IsInRole(Roles.Admin)
        }, cancellationToken));

    // Kapıda okutma: geçerliyse 200 ve koltuk bilgisi; kullanılmışsa 409, iptal edilmişse 422, kod yoksa 404.
    [Authorize(Policy = Policies.GateStaff)]
    [HttpPost("check-in")]
    public async Task<ActionResult<CheckInTicketResponse>> CheckIn([FromBody] CheckInTicketCommand command, CancellationToken cancellationToken)
    {
        command.StaffId = User.FindFirst("sub")?.Value
            ?? throw UnauthorizedException.MissingClaim("sub");

        return Ok(await _mediator.Send(command, cancellationToken));
    }
}
