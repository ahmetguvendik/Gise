using Gise.Order.Application.Features.Queries.Order.Response;
using MediatR;

namespace Gise.Order.Application.Features.Queries.Order.Request;

// Müşterinin siparişleri, en yenisi önce. Controller CustomerId'yi token'dan doldurur.
public sealed class GetMyOrdersQuery : IRequest<IReadOnlyList<GetMyOrdersResponse>>
{
    public string CustomerId { get; set; } = string.Empty;
}
