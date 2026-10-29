using Gise.Exceptions;
using Gise.Ticket.Application.Features.Commands.Ticket.Request;
using Gise.Ticket.Application.Features.Commands.Ticket.Response;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Application.Tickets;
using Gise.Ticket.Domain.Enums;
using MediatR;

namespace Gise.Ticket.Application.Features.Handlers.Ticket.CommandHandlers;

public sealed class CheckInTicketCommandHandler : IRequestHandler<CheckInTicketCommand, CheckInTicketResponse>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICancelledEventRepository _cancelledEventRepository;
    private readonly TimeProvider _timeProvider;

    public CheckInTicketCommandHandler(
        ITicketRepository ticketRepository,
        ICancelledEventRepository cancelledEventRepository,
        TimeProvider timeProvider)
    {
        _ticketRepository = ticketRepository;
        _cancelledEventRepository = cancelledEventRepository;
        _timeProvider = timeProvider;
    }

    public async Task<CheckInTicketResponse> Handle(CheckInTicketCommand request, CancellationToken cancellationToken)
    {
        // Biçimi CheckInTicketCommandValidator doğruladı.
        var code = TicketCode.Normalize(request.Code)!;
        var ticket = await _ticketRepository.GetByCodeAsync(code, cancellationToken)
            ?? throw NotFoundException.TicketCode();

        if (ticket.Status == TicketStatus.Cancelled)
            throw BusinessException.TicketCancelled(ticket.Id);

        if (ticket.Status == TicketStatus.Used)
            throw ConflictException.TicketAlreadyUsed(ticket.CheckedInAt!.Value);

        // Etkinlik iptali biletleri inbox worker'da iptal eder; iptalle aynı anda çıkan bir bilet geçerli kalmış olabilir.
        // Kapı son kontrol noktası: böyle bir bilet burada iptal edilir.
        if (await _cancelledEventRepository.ExistsAsync(ticket.EventId, cancellationToken))
        {
            await _ticketRepository.CancelAsync(ticket.Id, cancellationToken);
            throw BusinessException.TicketCancelled(ticket.Id);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (!await _ticketRepository.TryCheckInAsync(ticket.Id, request.StaffId, now, cancellationToken))
        {
            // Bu arada başka bir kapıda okutuldu ya da iptal edildi.
            var current = await _ticketRepository.GetByIdAsync(ticket.Id, cancellationToken);
            if (current?.Status == TicketStatus.Cancelled)
                throw BusinessException.TicketCancelled(ticket.Id);

            throw ConflictException.TicketAlreadyUsed(current?.CheckedInAt ?? now);
        }

        return new CheckInTicketResponse
        {
            TicketId = ticket.Id,
            EventTitle = ticket.EventTitle,
            EventStartsAt = ticket.EventStartsAt,
            SectionName = ticket.SectionName,
            Row = ticket.Row,
            Number = ticket.Number,
            CheckedInAt = now
        };
    }
}
