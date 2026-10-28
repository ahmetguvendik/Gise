using Gise.Exceptions;
using Gise.Order.Application.Features.Commands.Order.Request;
using Gise.Order.Application.Interfaces;
using Gise.Order.Application.Sagas.Messages;
using Gise.Order.Application.UnitOfWorks;
using Gise.Order.Domain.Enums;
using MassTransit;
using MediatR;

namespace Gise.Order.Application.Features.Handlers.Order.CommandHandlers;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(IOrderRepository orderRepository, IPublishEndpoint publishEndpoint, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw NotFoundException.Order(request.OrderId);

        if (!request.IsAdmin && order.CustomerId != request.CustomerId)
            throw ForbiddenException.NotOrderOwner(order.Id);

        // Koltuklar tutulmadan (Pending) vazgeçilemez; kapanmış siparişte de anlamı yok.
        if (order.Status != OrderStatus.AwaitingPayment)
            throw BusinessException.OrderNotCancellable(order.Id);

        // Durumu saga değiştirir (koltukları bırakıp iptal eder).
        await _publishEndpoint.Publish(new OrderCancellationRequested { OrderId = order.Id }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
