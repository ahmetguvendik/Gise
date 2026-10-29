using Gise.Exceptions;
using Gise.Order.Application.Features.Commands.Order.Request;
using Gise.Order.Application.Features.Commands.Order.Response;
using Gise.Order.Application.Interfaces;
using Gise.Order.Application.Sagas.Messages;
using Gise.Order.Application.UnitOfWorks;
using Gise.Order.Domain.Enums;
using MassTransit;
using MediatR;

namespace Gise.Order.Application.Features.Handlers.Order.CommandHandlers;

public sealed class PayOrderCommandHandler : IRequestHandler<PayOrderCommand, PayOrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public PayOrderCommandHandler(
        IOrderRepository orderRepository,
        IPublishEndpoint publishEndpoint,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PayOrderResponse> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw NotFoundException.Order(request.OrderId);

        // Siparişi yalnızca sahibi öder.
        if (order.CustomerId != request.CustomerId)
            throw ForbiddenException.NotOrderOwner(order.Id);

        // Koltuklar tutulmuş, başka bir ödeme sürmüyor ve süre dolmamış olmalı. Kart reddedildiyse sipariş yeniden
        // "ödeme bekleniyor"a döner; müşteri süre dolana kadar yeniden dener.
        if (order.Status != OrderStatus.AwaitingPayment || order.ExpiresAt <= _timeProvider.GetUtcNow().UtcDateTime)
            throw BusinessException.OrderNotPayable(order.Id);

        // Durumu saga değiştirir (ödemeyi başlatır). Bus outbox: mesaj SaveChanges'te yazılır, order-saga-worker gönderir.
        var paymentId = Guid.CreateVersion7();
        await _publishEndpoint.Publish(new OrderPaymentRequested
        {
            OrderId = order.Id,
            PaymentId = paymentId,
            CardLast4 = request.CardNumber[^4..]
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PayOrderResponse { PaymentId = paymentId };
    }
}
