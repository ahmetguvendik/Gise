using Gise.Exceptions;
using Gise.Authentication;
using Gise.Order.Application.Features.Commands.Order.Request;
using Gise.Order.Application.Features.Commands.Order.Response;
using Gise.Order.Application.Features.Queries.Order.Request;
using Gise.Order.Application.Features.Queries.Order.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Order.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // 202: sipariş açıldı, koltuklar arkada tutuluyor; sonucu GET {id} gösterir.
    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        command.CustomerId = User.FindFirst("sub")?.Value
            ?? throw UnauthorizedException.MissingClaim("sub");
        // Bilet e-postası için; token'da yoksa bildirim gönderilmez, sipariş yine açılır.
        command.CustomerEmail = User.FindFirst("email")?.Value ?? string.Empty;
        command.CustomerName = User.FindFirst("name")?.Value ?? string.Empty;

        var result = await _mediator.Send(command, cancellationToken);
        return AcceptedAtAction(nameof(GetById), new { id = result.OrderId }, result);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<GetMyOrdersResponse>>> GetMine(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMyOrdersQuery
        {
            CustomerId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub")
        }, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetOrderByIdResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetOrderByIdQuery
        {
            OrderId = id,
            CustomerId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub"),
            IsAdmin = User.IsInRole(Roles.Admin)
        }, cancellationToken));

    // 202: ödeme isteği alındı; ödeme arkada yapılır, sonucu GET {id} gösterir.
    [HttpPost("{id:guid}/pay")]
    public async Task<ActionResult<PayOrderResponse>> Pay(Guid id, [FromBody] PayOrderCommand command, CancellationToken cancellationToken)
    {
        command.OrderId = id;
        command.CustomerId = User.FindFirst("sub")?.Value
            ?? throw UnauthorizedException.MissingClaim("sub");

        var result = await _mediator.Send(command, cancellationToken);
        return AcceptedAtAction(nameof(GetById), new { id }, result);
    }

    // 202: vazgeçme isteği alındı; koltuklar arkada bırakılır, sipariş iptal olur.
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelOrderCommand
        {
            OrderId = id,
            CustomerId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub"),
            IsAdmin = User.IsInRole(Roles.Admin)
        }, cancellationToken);

        return Accepted();
    }
}
