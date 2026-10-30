using Gise.Exceptions;
using Gise.Ticket.Application.Features.Commands.TicketTransfer.Request;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Application.Tickets;
using Gise.Ticket.Application.UnitOfWorks;
using Gise.Ticket.Domain.Enums;
using MediatR;

namespace Gise.Ticket.Application.Features.Handlers.TicketTransfer.CommandHandlers;

public sealed class DeclineTicketTransferCommandHandler : IRequestHandler<DeclineTicketTransferCommand>
{
    private readonly ITicketTransferRepository _transferRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public DeclineTicketTransferCommandHandler(ITicketTransferRepository transferRepository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _transferRepository = transferRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(DeclineTicketTransferCommand request, CancellationToken cancellationToken)
    {
        // Doğrulanmamış adres başkasınınki olabilir; ona gönderilmiş bilete dokunulmaz.
        if (!request.EmailVerified)
            throw ForbiddenException.EmailNotVerified();

        var transfer = await _transferRepository.GetByIdAsync(request.TransferId, cancellationToken)
            ?? throw NotFoundException.TicketTransfer(request.TransferId);

        if (transfer.ToEmail != TicketTransferRules.NormalizeEmail(request.Email))
            throw ForbiddenException.NotTransferRecipient(transfer.Id);

        if (transfer.Status != TicketTransferStatus.Pending)
            throw ConflictException.TicketTransferNotPending(transfer.Id);

        transfer.Status = TicketTransferStatus.Declined;
        transfer.RespondedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
