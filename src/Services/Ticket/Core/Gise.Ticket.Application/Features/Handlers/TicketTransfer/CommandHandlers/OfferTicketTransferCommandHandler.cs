using Gise.Contracts.Events;
using Gise.Exceptions;
using Gise.Ticket.Application.Features.Commands.TicketTransfer.Request;
using Gise.Ticket.Application.Features.Commands.TicketTransfer.Response;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Application.Tickets;
using Gise.Ticket.Application.UnitOfWorks;
using Gise.Ticket.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Gise.Ticket.Application.Features.Handlers.TicketTransfer.CommandHandlers;

public sealed class OfferTicketTransferCommandHandler : IRequestHandler<OfferTicketTransferCommand, OfferTicketTransferResponse>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketTransferRepository _transferRepository;
    private readonly ICancelledEventRepository _cancelledEventRepository;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly int _cutoffHours;

    public OfferTicketTransferCommandHandler(
        ITicketRepository ticketRepository,
        ITicketTransferRepository transferRepository,
        ICancelledEventRepository cancelledEventRepository,
        IOutboxMessageRepository outboxMessageRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IConfiguration configuration)
    {
        _ticketRepository = ticketRepository;
        _transferRepository = transferRepository;
        _cancelledEventRepository = cancelledEventRepository;
        _outboxMessageRepository = outboxMessageRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _cutoffHours = TicketTransferRules.CutoffHours(configuration);
    }

    public async Task<OfferTicketTransferResponse> Handle(OfferTicketTransferCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken)
            ?? throw NotFoundException.Ticket(request.TicketId);

        if (ticket.CustomerId != request.CustomerId)
            throw ForbiddenException.NotTicketOwner(ticket.Id);

        if (ticket.Status == TicketStatus.Cancelled || await _cancelledEventRepository.ExistsAsync(ticket.EventId, cancellationToken))
            throw BusinessException.TicketCancelled(ticket.Id);

        if (ticket.Status == TicketStatus.Used)
            throw BusinessException.TicketNotTransferable(ticket.Id);

        if (ticket.TransferredAt is not null)
            throw BusinessException.TicketAlreadyTransferred(ticket.Id);

        var deadline = TicketTransferRules.Deadline(ticket, _cutoffHours);
        if (_timeProvider.GetUtcNow().UtcDateTime >= deadline)
            throw BusinessException.TicketTransferClosed(ticket.Id, _cutoffHours);

        var toEmail = TicketTransferRules.NormalizeEmail(request.Email);
        var fromEmail = TicketTransferRules.NormalizeEmail(request.CustomerEmail);
        if (toEmail == fromEmail)
            throw BusinessException.TicketTransferToSelf();

        // Veritabanında da tekil indeks var: bir biletin aynı anda tek bekleyen aktarımı olur.
        if (await _transferRepository.GetPendingForTicketAsync(ticket.Id, cancellationToken) is not null)
            throw ConflictException.TicketTransferPending(ticket.Id);

        var transfer = new Domain.Entities.TicketTransfer
        {
            TicketId = ticket.Id,
            EventId = ticket.EventId,
            FromCustomerId = request.CustomerId,
            FromEmail = fromEmail,
            FromName = request.CustomerName,
            ToEmail = toEmail
        };
        await _transferRepository.AddAsync(transfer, cancellationToken);

        // Alıcıya e-posta Notification'dan gider; aktarımla aynı SaveChanges'te outbox'a yazılır.
        await _outboxMessageRepository.AddTicketEventAsync(ticket.EventId, new TicketTransferOffered
        {
            TransferId = transfer.Id,
            TicketId = ticket.Id,
            EventId = ticket.EventId,
            FromCustomerId = request.CustomerId,
            FromName = request.CustomerName,
            ToEmail = toEmail,
            EventTitle = ticket.EventTitle,
            EventStartsAt = ticket.EventStartsAt,
            VenueName = ticket.VenueName,
            City = ticket.City,
            SectionName = ticket.SectionName,
            Row = ticket.Row,
            Number = ticket.Number,
            ExpiresAt = deadline
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OfferTicketTransferResponse { TransferId = transfer.Id, ToEmail = toEmail, ExpiresAt = deadline };
    }
}
