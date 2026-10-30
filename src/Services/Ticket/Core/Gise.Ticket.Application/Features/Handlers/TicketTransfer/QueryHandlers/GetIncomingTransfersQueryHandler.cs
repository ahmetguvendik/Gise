using Gise.Ticket.Application.Features.Queries.TicketTransfer.Request;
using Gise.Ticket.Application.Features.Queries.TicketTransfer.Response;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Application.Tickets;
using Gise.Ticket.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Gise.Ticket.Application.Features.Handlers.TicketTransfer.QueryHandlers;

public sealed class GetIncomingTransfersQueryHandler : IRequestHandler<GetIncomingTransfersQuery, IReadOnlyList<GetIncomingTransfersResponse>>
{
    private readonly ITicketTransferRepository _transferRepository;
    private readonly TimeProvider _timeProvider;
    private readonly int _cutoffHours;

    public GetIncomingTransfersQueryHandler(ITicketTransferRepository transferRepository, TimeProvider timeProvider, IConfiguration configuration)
    {
        _transferRepository = transferRepository;
        _timeProvider = timeProvider;
        _cutoffHours = TicketTransferRules.CutoffHours(configuration);
    }

    public async Task<IReadOnlyList<GetIncomingTransfersResponse>> Handle(GetIncomingTransfersQuery request, CancellationToken cancellationToken)
    {
        if (!request.EmailVerified)
            return [];

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var pending = await _transferRepository.GetPendingForEmailAsync(TicketTransferRules.NormalizeEmail(request.Email), cancellationToken);

        // Süresi dolan, bileti okutulan ya da iptal edilen aktarım listelenmez; kabul edilse de reddedilirdi.
        return pending
            .Where(x => x.Ticket.Status == TicketStatus.Valid
                && x.Ticket.CustomerId == x.Transfer.FromCustomerId
                && now < TicketTransferRules.Deadline(x.Ticket, _cutoffHours))
            .Select(x => new GetIncomingTransfersResponse
            {
                TransferId = x.Transfer.Id,
                TicketId = x.Ticket.Id,
                FromName = x.Transfer.FromName,
                EventId = x.Ticket.EventId,
                EventTitle = x.Ticket.EventTitle,
                EventStartsAt = x.Ticket.EventStartsAt,
                VenueName = x.Ticket.VenueName,
                City = x.Ticket.City,
                SectionName = x.Ticket.SectionName,
                Row = x.Ticket.Row,
                Number = x.Ticket.Number,
                ExpiresAt = TicketTransferRules.Deadline(x.Ticket, _cutoffHours)
            })
            .ToList();
    }
}
