using Gise.Contracts.Commands;
using Gise.Payment.Application.Features.Commands.Payment.Request;
using MassTransit;
using MediatR;

namespace Gise.Payment.CommandWorker.Messaging;

// Saga'nın telafi adımı: sipariş kapandı, çekilen para iade edilir.
public sealed class RefundPaymentConsumer : IConsumer<RefundPayment>
{
    private readonly IMediator _mediator;

    public RefundPaymentConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task Consume(ConsumeContext<RefundPayment> context)
        => _mediator.Send(new RefundPaymentCommand { OrderId = context.Message.OrderId }, context.CancellationToken);
}
