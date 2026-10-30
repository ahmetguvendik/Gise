using Gise.Ticket.Application.Features.Queries.Ticket.Request;
using Gise.Ticket.Application.Features.Queries.Ticket.Response;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Application.Tickets;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Gise.Ticket.Application.Features.Handlers.Ticket.QueryHandlers;

public sealed class GetGateEventsQueryHandler : IRequestHandler<GetGateEventsQuery, IReadOnlyList<GetGateEventsResponse>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly TimeProvider _timeProvider;
    private readonly CheckInWindow _window;

    public GetGateEventsQueryHandler(ITicketRepository ticketRepository, TimeProvider timeProvider, IConfiguration configuration)
    {
        _ticketRepository = ticketRepository;
        _timeProvider = timeProvider;
        _window = CheckInWindow.FromConfiguration(configuration);
    }

    // Kapısı açık: başlangıcına OpensHoursBefore'dan az kalmış ya da ClosesHoursAfter'dan kısa süre önce başlamış.
    public async Task<IReadOnlyList<GetGateEventsResponse>> Handle(GetGateEventsQuery request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var events = await _ticketRepository.GetGateEventsAsync(
            now.AddHours(-_window.ClosesHoursAfter), now.AddHours(_window.OpensHoursBefore), cancellationToken);

        return events.Select(e => new GetGateEventsResponse
        {
            EventId = e.EventId,
            EventTitle = e.EventTitle,
            EventStartsAt = e.EventStartsAt,
            VenueName = e.VenueName,
            City = e.City,
            OpensAt = _window.OpensAt(e.EventStartsAt),
            ClosesAt = _window.ClosesAt(e.EventStartsAt),
            TicketCount = e.TicketCount,
            CheckedInCount = e.CheckedInCount
        }).ToList();
    }
}
