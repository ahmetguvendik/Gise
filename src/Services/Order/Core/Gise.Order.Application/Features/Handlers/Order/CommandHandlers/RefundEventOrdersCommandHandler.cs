using Gise.Contracts.Commands;
using Gise.Order.Application.Features.Commands.Order.Request;
using Gise.Order.Application.Interfaces;
using Gise.Order.Application.UnitOfWorks;
using Gise.Order.Domain.Enums;
using MassTransit;
using MediatR;

namespace Gise.Order.Application.Features.Handlers.Order.CommandHandlers;

public sealed class RefundEventOrdersCommandHandler : IRequestHandler<RefundEventOrdersCommand>
{
    private static readonly Uri RefundPaymentQueue = new($"queue:{Queues.RefundPayment}");

    private readonly IOrderRepository _orderRepository;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefundEventOrdersCommandHandler(IOrderRepository orderRepository, ISendEndpointProvider sendEndpointProvider, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _sendEndpointProvider = sendEndpointProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RefundEventOrdersCommand request, CancellationToken cancellationToken)
    {
        // Olay ikinci kez gelirse iade edilmiş siparişler artık "tamamlanmış" değildir; tekrar iade gitmez.
        var orders = await _orderRepository.GetCompletedByEventForUpdateAsync(request.EventId, cancellationToken);
        if (orders.Count == 0)
            return;

        // Bus outbox: komutlar siparişlerle aynı SaveChanges'te yazılır, order-saga-worker gönderir.
        var refundEndpoint = await _sendEndpointProvider.GetSendEndpoint(RefundPaymentQueue);
        foreach (var order in orders)
        {
            order.Status = OrderStatus.Refunded;
            order.FailureReason = "Etkinlik iptal edildi; ödemeniz iade edildi.";
            await refundEndpoint.Send(new RefundPayment { OrderId = order.Id }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
