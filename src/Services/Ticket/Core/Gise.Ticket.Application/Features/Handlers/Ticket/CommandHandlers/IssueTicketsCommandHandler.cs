using Gise.Contracts.Events;
using Gise.Exceptions;
using Gise.Ticket.Application.Features.Commands.Ticket.Request;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Application.Tickets;
using Gise.Ticket.Application.UnitOfWorks;
using MediatR;

namespace Gise.Ticket.Application.Features.Handlers.Ticket.CommandHandlers;

public sealed class IssueTicketsCommandHandler : IRequestHandler<IssueTicketsCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICancelledEventRepository _cancelledEventRepository;
    private readonly ITicketPdfRenderer _pdfRenderer;
    private readonly ITicketStorage _ticketStorage;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IssueTicketsCommandHandler(
        ITicketRepository ticketRepository,
        ICancelledEventRepository cancelledEventRepository,
        ITicketPdfRenderer pdfRenderer,
        ITicketStorage ticketStorage,
        IOutboxMessageRepository outboxMessageRepository,
        IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _cancelledEventRepository = cancelledEventRepository;
        _pdfRenderer = pdfRenderer;
        _ticketStorage = ticketStorage;
        _outboxMessageRepository = outboxMessageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(IssueTicketsCommand request, CancellationToken cancellationToken)
    {
        // Aynı komut ikinci kez gelirse biletler zaten çıkmıştır.
        if (await _ticketRepository.AnyForOrderAsync(request.OrderId, cancellationToken))
            return;

        // Etkinlik ödeme sürerken iptal edildiyse bilet çıkmaz; sipariş iptal olur, ödeme iade edilir.
        if (await _cancelledEventRepository.ExistsAsync(request.EventId, cancellationToken))
            throw BusinessException.TicketEventCancelled(request.EventId);

        // Koltuğun geçerli bileti varsa ikincisi çıkmaz; veritabanında da tekil indeks var.
        if (await _ticketRepository.AnyActiveForSeatsAsync(request.EventId, request.Seats.Select(s => s.SeatId).ToList(), cancellationToken))
            throw ConflictException.TicketSeatTaken();

        var tickets = request.Seats.Select(seat =>
        {
            var id = Guid.CreateVersion7();
            return new Domain.Entities.Ticket
            {
                Id = id,
                OrderId = request.OrderId,
                EventId = request.EventId,
                SeatId = seat.SeatId,
                CustomerId = request.CustomerId,
                EventTitle = request.EventTitle,
                EventStartsAt = request.EventStartsAt,
                VenueName = request.VenueName,
                City = request.City,
                SectionName = seat.SectionName,
                Row = seat.Row,
                Number = seat.Number,
                Price = seat.Price,
                Code = TicketCode.Generate(),
                PdfKey = $"tickets/{request.EventId}/{id}.pdf"
            };
        }).ToList();

        // PDF'ler kayıttan önce yüklenir. Kayıt başarısız olursa komut yeniden denenir; yüklenen dosya sahipsiz kalır
        // ama herkese açık değildir.
        foreach (var ticket in tickets)
            await _ticketStorage.UploadPdfAsync(ticket.PdfKey, _pdfRenderer.Render(ticket), cancellationToken);

        await _ticketRepository.AddRangeAsync(tickets, cancellationToken);

        // Notification'ın e-postası için; biletlerle aynı SaveChanges'te outbox'a yazılır.
        await _outboxMessageRepository.AddTicketEventAsync(request.EventId, new TicketsIssued
        {
            OrderId = request.OrderId,
            EventId = request.EventId,
            CustomerId = request.CustomerId,
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            EventTitle = request.EventTitle,
            EventStartsAt = request.EventStartsAt,
            VenueName = request.VenueName,
            City = request.City,
            Tickets = tickets.Select(t => new IssuedTicket { SectionName = t.SectionName, Row = t.Row, Number = t.Number }).ToList()
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
