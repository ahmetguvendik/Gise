using Gise.Order.Application.Features.Queries.Order.Request;
using Gise.Order.Application.Features.Queries.Order.Response;
using Gise.Order.Application.Interfaces;
using MediatR;

namespace Gise.Order.Application.Features.Handlers.Order.QueryHandlers;

public sealed class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, IReadOnlyList<GetMyOrdersResponse>>
{
    private readonly IOrderRepository _orderRepository;

    public GetMyOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IReadOnlyList<GetMyOrdersResponse>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetByCustomerAsync(request.CustomerId, cancellationToken);

        return orders
            .Select(o => new GetMyOrdersResponse
            {
                Id = o.Id,
                EventId = o.EventId,
                Status = o.Status.ToString(),
                TotalAmount = o.TotalAmount,
                SeatCount = o.Items.Count,
                ExpiresAt = o.ExpiresAt,
                CreatedAt = o.CreatedAt
            })
            .ToList();
    }
}
