using Gise.Contracts.Commands;
using Gise.Payment.Application.Features.Commands.Payment.Request;
using Gise.Payment.Domain.Enums;
using MassTransit;
using MediatR;

namespace Gise.Payment.CommandWorker.Messaging;

// Satın alma saga'sının ödeme adımı: denemenin parasını çeker, sonucu Order'a bildirir. Sağlayıcının geçici hatası
// (test kartı 0009'un ilk denemesi) yeniden denenir.
public sealed class ProcessPaymentConsumer : IConsumer<ProcessPayment>
{
    private readonly IMediator _mediator;

    public ProcessPaymentConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        var message = context.Message;
        var result = await _mediator.Send(new ProcessPaymentCommand
        {
            PaymentId = message.PaymentId,
            OrderId = message.OrderId,
            Amount = message.Amount,
            CardLast4 = message.CardLast4
        }, context.CancellationToken);

        switch (result.Status)
        {
            case PaymentStatus.Succeeded:
                await context.Publish(new PaymentSucceeded
                {
                    PaymentId = message.PaymentId,
                    OrderId = message.OrderId,
                    Amount = message.Amount
                });
                break;

            // Sağlayıcı cevap vermedi (test kartı 0005): Order'a da cevap gitmez, saga'nın zaman aşımı devreye girer.
            case PaymentStatus.Pending:
                break;

            default:
                await context.Publish(new PaymentFailed
                {
                    PaymentId = message.PaymentId,
                    OrderId = message.OrderId,
                    Reason = result.FailureReason ?? "Ödeme alınamadı."
                });
                break;
        }
    }
}
