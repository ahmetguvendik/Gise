using MediatR;

namespace Gise.Order.Application.Features.Commands.Order.Request;

// Ödeme beklenirken siparişten vazgeçme; koltukları saga bırakır.
public sealed class CancelOrderCommand : IRequest
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
