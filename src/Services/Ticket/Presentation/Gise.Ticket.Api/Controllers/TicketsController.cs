using Gise.Exceptions;
using Gise.Authentication;
using Gise.Ticket.Api.Authentication;
using Gise.Ticket.Application.Features.Commands.Ticket.Request;
using Gise.Ticket.Application.Features.Commands.Ticket.Response;
using Gise.Ticket.Application.Features.Commands.TicketTransfer.Request;
using Gise.Ticket.Application.Features.Commands.TicketTransfer.Response;
using Gise.Ticket.Application.Features.Queries.Ticket.Request;
using Gise.Ticket.Application.Features.Queries.Ticket.Response;
using Gise.Ticket.Application.Features.Queries.TicketTransfer.Request;
using Gise.Ticket.Application.Features.Queries.TicketTransfer.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Ticket.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    // TicketTransferConfiguration'daki FromName sütunuyla aynı; Keycloak ada daha uzununa izin veriyor.
    private const int MaxNameLength = 200;

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

    // Kapı sayfası: şu an okutması açık etkinlikler, bilet ve giriş sayılarıyla.
    [Authorize(Policy = Policies.GateStaff)]
    [HttpGet("gate/events")]
    public async Task<ActionResult<IReadOnlyList<GetGateEventsResponse>>> GetGateEvents(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetGateEventsQuery(), cancellationToken));

    // Kapıda okutma: geçerliyse 200 ve koltuk bilgisi; kullanılmışsa 409; iptal edilmiş, başka etkinliğin ya da okutma
    // penceresi dışındaysa 422; kod yoksa 404.
    [Authorize(Policy = Policies.GateStaff)]
    [HttpPost("check-in")]
    public async Task<ActionResult<CheckInTicketResponse>> CheckIn([FromBody] CheckInTicketCommand command, CancellationToken cancellationToken)
    {
        command.StaffId = User.FindFirst("sub")?.Value
            ?? throw UnauthorizedException.MissingClaim("sub");

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    // Bileti bir e-posta adresine aktarma teklifi; alıcı o adresle giriş yapıp kabul edince bilet ona geçer.
    [HttpPost("{id:guid}/transfer")]
    public async Task<ActionResult<OfferTicketTransferResponse>> OfferTransfer(
        Guid id, [FromBody] OfferTicketTransferCommand command, CancellationToken cancellationToken)
    {
        command.TicketId = id;
        command.CustomerId = CustomerId;
        command.CustomerEmail = User.FindFirst("email")?.Value ?? string.Empty;
        command.CustomerName = Name;

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    // Alıcı kabul etmeden önce gönderen geri alabilir.
    [HttpPost("{id:guid}/transfer/cancel")]
    public async Task<IActionResult> CancelTransfer(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelTicketTransferCommand { TicketId = id, CustomerId = CustomerId }, cancellationToken);
        return NoContent();
    }

    // Giriş yapanın doğrulanmış e-postasına gönderilmiş, hâlâ kabul edilebilir aktarımlar.
    [HttpGet("transfers/incoming")]
    public async Task<ActionResult<IReadOnlyList<GetIncomingTransfersResponse>>> GetIncomingTransfers(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetIncomingTransfersQuery
        {
            Email = User.FindFirst("email")?.Value ?? string.Empty,
            EmailVerified = EmailVerified
        }, cancellationToken));

    // Kabul edilince bilet giriş yapana geçer; yeni kod ve PDF verilir, eskisi geçersiz olur.
    [HttpPost("transfers/{transferId:guid}/accept")]
    public async Task<ActionResult<AcceptTicketTransferResponse>> AcceptTransfer(Guid transferId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new AcceptTicketTransferCommand
        {
            TransferId = transferId,
            CustomerId = CustomerId,
            Email = User.FindFirst("email")?.Value ?? throw UnauthorizedException.MissingClaim("email"),
            EmailVerified = EmailVerified,
            Name = Name
        }, cancellationToken));

    [HttpPost("transfers/{transferId:guid}/decline")]
    public async Task<IActionResult> DeclineTransfer(Guid transferId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeclineTicketTransferCommand
        {
            TransferId = transferId,
            Email = User.FindFirst("email")?.Value ?? throw UnauthorizedException.MissingClaim("email"),
            EmailVerified = EmailVerified
        }, cancellationToken);
        return NoContent();
    }

    private string CustomerId => User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub");

    // Keycloak e-postayı doğrulamadan giriş izni vermiyor (verifyEmail); yine de token'daki bilgiye bakılır.
    private bool EmailVerified => string.Equals(User.FindFirst("email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase);

    // Ad yalnızca e-postalarda ve "kimden" bilgisinde kullanılır; sütuna sığacak kadarı alınır.
    private string Name
    {
        get
        {
            var name = User.FindFirst("name")?.Value ?? string.Empty;
            return name.Length > MaxNameLength ? name[..MaxNameLength] : name;
        }
    }
}
