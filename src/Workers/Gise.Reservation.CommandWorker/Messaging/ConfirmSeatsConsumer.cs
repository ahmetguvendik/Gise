using Gise.Contracts.Commands;
using Gise.Exceptions;
using Gise.Reservation.Application.Features.Commands.SeatHold.Request;
using MassTransit;
using MediatR;

namespace Gise.Reservation.CommandWorker.Messaging;

// Ödeme alındıktan sonraki adım: siparişin tuttuğu koltukları satar, sonucu Order'a bildirir.
public sealed class ConfirmSeatsConsumer : IConsumer<ConfirmSeats>
{
    private readonly IMediator _mediator;

    public ConfirmSeatsConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<ConfirmSeats> context)
    {
        try
        {
            await _mediator.Send(new ConfirmHoldCommand { OrderId = context.Message.OrderId }, context.CancellationToken);
            await context.Publish(new SeatsConfirmed { OrderId = context.Message.OrderId });
        }
        catch (AppException ex)
        {
            // İş kuralı (tutma bırakılmış, koltuk satılmış): yeniden denemenin anlamı yok, sipariş iptal olur ve ödeme
            // iade edilir. Diğer hatalar (Redis, veritabanı, eşzamanlı satış çakışması) yeniden denenir.
            await context.Publish(new SeatConfirmationFailed
            {
                OrderId = context.Message.OrderId,
                Reason = ex.Message
            });
        }   
    }
}
