using Gise.Ticket.Application.Features.Queries.Ticket.Request;
using Gise.Ticket.Application.Features.Queries.Ticket.Response;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Application.Tickets;
using MediatR;

namespace Gise.Ticket.Application.Features.Handlers.Ticket.QueryHandlers;

public sealed class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, IReadOnlyList<GetMyTicketsResponse>>
{
    private readonly ITicketRepository _ticketRepository;

    public GetMyTicketsQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<IReadOnlyList<GetMyTicketsResponse>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
    {
        var tickets = await _ticketRepository.GetByCustomerAsync(request.CustomerId, request.OrderId, cancellationToken);

        return tickets.Select(t => new GetMyTicketsResponse
        {
            Id = t.Id,
            OrderId = t.OrderId,
            EventId = t.EventId,
            EventTitle = t.EventTitle,
            EventStartsAt = t.EventStartsAt,
            VenueName = t.VenueName,
            City = t.City,
            SectionName = t.SectionName,
            Row = t.Row,
            Number = t.Number,
            Price = t.Price,
            Code = TicketCode.Format(t.Code),
            Status = t.Status.ToString(),
            CheckedInAt = t.CheckedInAt
        }).ToList();
    }
}
