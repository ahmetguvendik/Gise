using Gise.Ticket.Application.Features.Queries.Ticket.Request;
using Gise.Ticket.Application.Features.Queries.Ticket.Response;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Application.Tickets;
using Gise.Ticket.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Gise.Ticket.Application.Features.Handlers.Ticket.QueryHandlers;

public sealed class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, IReadOnlyList<GetMyTicketsResponse>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketTransferRepository _transferRepository;
    private readonly TimeProvider _timeProvider;
    private readonly int _cutoffHours;

    public GetMyTicketsQueryHandler(
        ITicketRepository ticketRepository,
        ITicketTransferRepository transferRepository,
        TimeProvider timeProvider,
        IConfiguration configuration)
    {
        _ticketRepository = ticketRepository;
        _transferRepository = transferRepository;
        _timeProvider = timeProvider;
        _cutoffHours = TicketTransferRules.CutoffHours(configuration);
    }

    public async Task<IReadOnlyList<GetMyTicketsResponse>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
    {
        var tickets = await _ticketRepository.GetByCustomerAsync(request.CustomerId, request.OrderId, cancellationToken);
        var pending = (await _transferRepository.GetPendingForTicketsAsync(tickets.Select(t => t.Id).ToList(), cancellationToken))
            .ToDictionary(t => t.TicketId);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return tickets.Select(t =>
        {
            var deadline = TicketTransferRules.Deadline(t, _cutoffHours);
            var open = t.Status == TicketStatus.Valid && now < deadline;
            // Süresi dolan aktarım düşmüş sayılır; bilet sahibinde kalır.
            var transfer = open ? pending.GetValueOrDefault(t.Id) : null;

            return new GetMyTicketsResponse
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
                CheckedInAt = t.CheckedInAt,
                TransferredAt = t.TransferredAt,
                CanTransfer = open && t.TransferredAt is null && transfer is null,
                TransferDeadline = deadline,
                PendingTransfer = transfer is null
                    ? null
                    : new PendingTransferResponse { Id = transfer.Id, ToEmail = transfer.ToEmail, CreatedAt = transfer.CreatedAt }
            };
        }).ToList();
    }
}
