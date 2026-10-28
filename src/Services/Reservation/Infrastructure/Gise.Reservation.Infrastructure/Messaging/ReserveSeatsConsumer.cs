using Gise.Contracts.Commands;
using Gise.Exceptions;
using Gise.Reservation.Application.Features.Commands.SeatHold.Request;
using MassTransit;
using MediatR;

namespace Gise.Reservation.Infrastructure.Messaging;

// Satın alma saga'sının ilk adımı: siparişin koltuklarını tutar, sonucu Order'a bildirir.
public sealed class ReserveSeatsConsumer : IConsumer<ReserveSeats>
{
    private readonly IMediator _mediator;

    public ReserveSeatsConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<ReserveSeats> context)
    {
        var message = context.Message;
        try
        {
            var hold = await _mediator.Send(new HoldSeatsCommand
            {
                OrderId = message.OrderId,
                EventId = message.EventId,
                SeatIds = message.SeatIds,
                UserId = message.CustomerId
            }, context.CancellationToken);

            await context.Publish(new SeatsReserved
            {
                OrderId = message.OrderId,
                ExpiresAt = hold.ExpiresAt,
                TotalPrice = hold.TotalPrice,
                Seats = hold.Seats.Select(seat => new ReservedSeat
                {
                    SeatId = seat.SeatId,
                    SectionName = seat.SectionName,
                    Row = seat.Row,
                    Number = seat.Number,
                    Price = seat.Price
                }).ToList()
            });
        }
        catch (AppException ex)
        {
            // İş kuralı (koltuk dolu, etkinlik satışta değil…): yeniden denemenin anlamı yok, sipariş iptal olur.
            // Diğer hatalar (Redis, veritabanı) yeniden denenir.
            await context.Publish(new SeatReservationFailed
            {
                OrderId = message.OrderId,
                ErrorCode = ex.ErrorCode,
                Reason = ex.Message
            });
        }
    }
}
