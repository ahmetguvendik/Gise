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
    // Biletin kendi durumu değil: gönderenin gözünden "başkasına aktarıldı".
    private const string TransferredStatus = "Transferred";

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

        var owned = tickets.Select(t =>
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
        });

        // Başkasına aktardığı biletler de listede kalır (sipariş sayfasında da); kodu verilmez, kapıdan girmek alıcının hakkı.
        var given = (await _transferRepository.GetAcceptedFromCustomerAsync(request.CustomerId, request.OrderId, cancellationToken))
            .Select(x => new GetMyTicketsResponse
            {
                Id = x.Ticket.Id,
                OrderId = x.Ticket.OrderId,
                EventId = x.Ticket.EventId,
                EventTitle = x.Ticket.EventTitle,
                EventStartsAt = x.Ticket.EventStartsAt,
                VenueName = x.Ticket.VenueName,
                City = x.Ticket.City,
                SectionName = x.Ticket.SectionName,
                Row = x.Ticket.Row,
                Number = x.Ticket.Number,
                Price = x.Ticket.Price,
                Status = TransferredStatus,
                TransferredAt = x.Transfer.RespondedAt,
                TransferredTo = x.Transfer.ToEmail,
                TransferDeadline = TicketTransferRules.Deadline(x.Ticket, _cutoffHours)
            });

        // Repository'nin sırası: en yakın etkinlik önce, sonra bölüm, sıra ve koltuk.
        return owned.Concat(given)
            .OrderBy(t => t.EventStartsAt)
            .ThenBy(t => t.SectionName)
            .ThenBy(t => t.Row.Length)
            .ThenBy(t => t.Row)
            .ThenBy(t => t.Number)
            .ToList();
    }
}
