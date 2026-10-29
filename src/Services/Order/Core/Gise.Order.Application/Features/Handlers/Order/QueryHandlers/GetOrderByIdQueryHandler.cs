using Gise.Exceptions;
using Gise.Order.Application.Features.Queries.Order.Request;
using Gise.Order.Application.Features.Queries.Order.Response;
using Gise.Order.Application.Interfaces;
using MediatR;

namespace Gise.Order.Application.Features.Handlers.Order.QueryHandlers;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, GetOrderByIdResponse>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<GetOrderByIdResponse> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw NotFoundException.Order(request.OrderId);

        if (!request.IsAdmin && order.CustomerId != request.CustomerId)
            throw ForbiddenException.NotOrderOwner(order.Id);

        return new GetOrderByIdResponse
        {
            Id = order.Id,
            EventId = order.EventId,
            EventTitle = order.EventTitle,
            EventStartsAt = order.EventStartsAt,
            VenueName = order.VenueName,
            City = order.City,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            ExpiresAt = order.ExpiresAt,
            FailureReason = order.FailureReason,
            LastPaymentId = order.LastPaymentId,
            CreatedAt = order.CreatedAt,
            Items = order.Items
                .OrderBy(i => i.SectionName)
                .ThenBy(i => i.Row.Length)
                .ThenBy(i => i.Row)
                .ThenBy(i => i.Number)
                .Select(i => new OrderItemResponse
                {
                    SeatId = i.SeatId,
                    SectionName = i.SectionName,
                    Row = i.Row,
                    Number = i.Number,
                    Price = i.Price
                })
                .ToList()
        };
    }
}
