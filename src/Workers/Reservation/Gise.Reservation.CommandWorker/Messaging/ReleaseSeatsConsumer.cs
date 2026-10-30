using Gise.Contracts.Commands;
using Gise.Reservation.Application.Features.Commands.SeatHold.Request;
using MassTransit;
using MediatR;

namespace Gise.Reservation.CommandWorker.Messaging;

// Saga'nın telafi adımı: müşteri vazgeçti ya da ödeme süresi doldu, koltuklar bırakılır.
public sealed class ReleaseSeatsConsumer : IConsumer<ReleaseSeats>
{
    private readonly IMediator _mediator;

    public ReleaseSeatsConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task Consume(ConsumeContext<ReleaseSeats> context)
        => _mediator.Send(new ReleaseHoldCommand
        {
            OrderId = context.Message.OrderId,
            EventId = context.Message.EventId,
            CustomerId = context.Message.CustomerId
        }, context.CancellationToken);
}
