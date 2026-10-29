using Gise.Contracts.Commands;
using Gise.Exceptions;
using Gise.Ticket.Application.Features.Commands.Ticket.Request;
using MassTransit;
using MediatR;

namespace Gise.Ticket.CommandWorker.Messaging;

// Satın alma saga'sının son adımı: siparişin biletlerini çıkarır, sonucu Order'a bildirir.
public sealed class IssueTicketsConsumer : IConsumer<IssueTickets>
{
    private readonly IMediator _mediator;

    public IssueTicketsConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<IssueTickets> context)
    {
        var message = context.Message;
        try
        {
            await _mediator.Send(new IssueTicketsCommand
            {
                OrderId = message.OrderId,
                EventId = message.EventId,
                CustomerId = message.CustomerId,
                CustomerEmail = message.CustomerEmail,
                CustomerName = message.CustomerName,
                EventTitle = message.EventTitle,
                EventStartsAt = message.EventStartsAt,
                VenueName = message.VenueName,
                City = message.City,
                Seats = message.Seats.Select(seat => new IssueTicketSeat
                {
                    SeatId = seat.SeatId,
                    SectionName = seat.SectionName,
                    Row = seat.Row,
                    Number = seat.Number,
                    Price = seat.Price
                }).ToList()
            }, context.CancellationToken);

            await context.Publish(new TicketsIssued { OrderId = message.OrderId });
        }
        catch (AppException ex)
        {
            // İş kuralı (etkinlik iptal edildi, koltuğun bileti var): yeniden denemenin anlamı yok; sipariş iptal olur,
            // ödeme iade edilir. Diğer hatalar (MinIO, veritabanı) yeniden denenir.
            await context.Publish(new TicketIssueFailed
            {
                OrderId = message.OrderId,
                Reason = ex.Message
            });
        }
    }
}
