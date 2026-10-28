using Gise.Order.Application.Features.Commands.Order.Request;
using Gise.Order.Application.Features.Commands.Order.Response;
using Gise.Order.Application.Interfaces;
using Gise.Order.Application.Sagas.Messages;
using Gise.Order.Application.UnitOfWorks;
using Gise.Order.Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Gise.Order.Application.Features.Handlers.Order.CommandHandlers;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _paymentWindow;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IPublishEndpoint publishEndpoint,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _paymentWindow = TimeSpan.FromMinutes(configuration.GetRequiredSection("OrderRules").GetValue<int>("PaymentWindowMinutes"));
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Domain.Entities.Order
        {
            CustomerId = request.CustomerId,
            EventId = request.EventId,
            Status = OrderStatus.Pending,
            // Koltuklar tutulunca tutmanın bitişine eşitlenir.
            ExpiresAt = _timeProvider.GetUtcNow().UtcDateTime.Add(_paymentWindow)
        };

        await _orderRepository.AddAsync(order, cancellationToken);

        // Saga'yı başlatır. Bus outbox: mesaj siparişle aynı SaveChanges'te yazılır; sipariş kaydedilmezse mesaj da gitmez.
        await _publishEndpoint.Publish(new OrderSubmitted
        {
            OrderId = order.Id,
            EventId = request.EventId,
            CustomerId = request.CustomerId,
            SeatIds = request.SeatIds
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateOrderResponse { OrderId = order.Id, Status = order.Status.ToString() };
    }
}
