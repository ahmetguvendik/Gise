using Gise.Order.Application.Features.Queries.Order.Response;
using MediatR;

namespace Gise.Order.Application.Features.Queries.Order.Request;

// Yalnızca siparişin sahibi ve admin görür.
public sealed class GetOrderByIdQuery : IRequest<GetOrderByIdResponse>
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
