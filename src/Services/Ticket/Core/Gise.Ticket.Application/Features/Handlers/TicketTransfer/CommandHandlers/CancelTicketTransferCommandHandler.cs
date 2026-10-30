using Gise.Exceptions;
using Gise.Ticket.Application.Features.Commands.TicketTransfer.Request;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Application.UnitOfWorks;
using Gise.Ticket.Domain.Enums;
using MediatR;

namespace Gise.Ticket.Application.Features.Handlers.TicketTransfer.CommandHandlers;

public sealed class CancelTicketTransferCommandHandler : IRequestHandler<CancelTicketTransferCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketTransferRepository _transferRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CancelTicketTransferCommandHandler(
        ITicketRepository ticketRepository,
        ITicketTransferRepository transferRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _ticketRepository = ticketRepository;
        _transferRepository = transferRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(CancelTicketTransferCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken)
            ?? throw NotFoundException.Ticket(request.TicketId);

        if (ticket.CustomerId != request.CustomerId)
            throw ForbiddenException.NotTicketOwner(ticket.Id);

        var transfer = await _transferRepository.GetPendingForTicketAsync(ticket.Id, cancellationToken)
            ?? throw NotFoundException.PendingTicketTransfer(ticket.Id);

        transfer.Status = TicketTransferStatus.Cancelled;
        transfer.RespondedAt = _timeProvider.GetUtcNow().UtcDateTime;

        // Alıcı aynı anda kabul ettiyse aktarımın sürümü (xmin) değişmiştir: 409, bilet artık alıcının.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
