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

public sealed class AcceptTicketTransferCommandHandler : IRequestHandler<AcceptTicketTransferCommand, AcceptTicketTransferResponse>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketTransferRepository _transferRepository;
    private readonly ICancelledEventRepository _cancelledEventRepository;
    private readonly ITicketPdfRenderer _pdfRenderer;
    private readonly ITicketStorage _ticketStorage;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly int _cutoffHours;

    public AcceptTicketTransferCommandHandler(
        ITicketRepository ticketRepository,
        ITicketTransferRepository transferRepository,
        ICancelledEventRepository cancelledEventRepository,
        ITicketPdfRenderer pdfRenderer,
        ITicketStorage ticketStorage,
        IOutboxMessageRepository outboxMessageRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IConfiguration configuration)
    {
        _ticketRepository = ticketRepository;
        _transferRepository = transferRepository;
        _cancelledEventRepository = cancelledEventRepository;
        _pdfRenderer = pdfRenderer;
        _ticketStorage = ticketStorage;
        _outboxMessageRepository = outboxMessageRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _cutoffHours = TicketTransferRules.CutoffHours(configuration);
    }

    public async Task<AcceptTicketTransferResponse> Handle(AcceptTicketTransferCommand request, CancellationToken cancellationToken)
    {
        // Doğrulanmamış adres başkasınınki olabilir: aynı adresle kaydolan biri bileti alamaz.
        if (!request.EmailVerified)
            throw ForbiddenException.EmailNotVerified();

        var transfer = await _transferRepository.GetByIdAsync(request.TransferId, cancellationToken)
            ?? throw NotFoundException.TicketTransfer(request.TransferId);

        if (transfer.ToEmail != TicketTransferRules.NormalizeEmail(request.Email))
            throw ForbiddenException.NotTransferRecipient(transfer.Id);

        if (transfer.Status != TicketTransferStatus.Pending)
            throw ConflictException.TicketTransferNotPending(transfer.Id);

        var ticket = await _ticketRepository.GetForUpdateAsync(transfer.TicketId, cancellationToken)
            ?? throw NotFoundException.Ticket(transfer.TicketId);

        // Teklif yapıldıktan sonra bilet okutulmuş, etkinlik iptal edilmiş ya da süre dolmuş olabilir.
        if (ticket.Status == TicketStatus.Cancelled || await _cancelledEventRepository.ExistsAsync(ticket.EventId, cancellationToken))
            throw BusinessException.TicketCancelled(ticket.Id);

        if (ticket.Status == TicketStatus.Used)
            throw BusinessException.TicketNotTransferable(ticket.Id);

        if (ticket.CustomerId != transfer.FromCustomerId || ticket.TransferredAt is not null)
            throw ConflictException.TicketTransferNotPending(transfer.Id);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (now >= TicketTransferRules.Deadline(ticket, _cutoffHours))
            throw BusinessException.TicketTransferClosed(ticket.Id, _cutoffHours);

        if (request.CustomerId == transfer.FromCustomerId)
            throw BusinessException.TicketTransferToSelf();

        // Yeni kod: gönderenin elindeki eski PDF ya da ekran görüntüsü kapıda geçmez.
        var previousPdfKey = ticket.PdfKey;
        ticket.CustomerId = request.CustomerId;
        ticket.Code = TicketCode.Generate();
        ticket.PdfKey = $"tickets/{ticket.EventId}/{ticket.Id}-{transfer.Id}.pdf";
        ticket.TransferredAt = now;

        // PDF kayıttan önce yüklenir (bkz. IssueTicketsCommandHandler); kayıt çakışırsa dosya sahipsiz kalır, herkese açık
        // değildir ve kodu hiçbir bilette yoktur.
        await _ticketStorage.UploadPdfAsync(ticket.PdfKey, _pdfRenderer.Render(ticket), cancellationToken);

        transfer.Status = TicketTransferStatus.Accepted;
        transfer.ToCustomerId = request.CustomerId;
        transfer.RespondedAt = now;

        // Notification bilet sahiplerini günceller ve gönderene yazar; biletle aynı SaveChanges'te outbox'a yazılır.
        await _outboxMessageRepository.AddTicketEventAsync(ticket.EventId, new TicketTransferred
        {
            TransferId = transfer.Id,
            TicketId = ticket.Id,
            OrderId = ticket.OrderId,
            EventId = ticket.EventId,
            FromCustomerId = transfer.FromCustomerId,
            FromEmail = transfer.FromEmail,
            FromName = transfer.FromName,
            ToCustomerId = request.CustomerId,
            ToEmail = transfer.ToEmail,
            ToName = request.Name,
            EventTitle = ticket.EventTitle,
            EventStartsAt = ticket.EventStartsAt,
            VenueName = ticket.VenueName,
            City = ticket.City,
            SectionName = ticket.SectionName,
            Row = ticket.Row,
            Number = ticket.Number
        }, cancellationToken);

        // Bilet ve aktarımın sürümü (xmin): bu arada bilet okutulduysa ya da aktarım geri alındıysa 409.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Eski PDF'teki QR artık geçersiz.
        await _ticketStorage.TryDeletePdfAsync(previousPdfKey, cancellationToken);

        return new AcceptTicketTransferResponse { TicketId = ticket.Id, Code = TicketCode.Format(ticket.Code) };
    }
}
