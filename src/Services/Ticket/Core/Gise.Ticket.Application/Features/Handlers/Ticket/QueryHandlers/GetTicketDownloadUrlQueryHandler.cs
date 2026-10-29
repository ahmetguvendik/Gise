using Gise.Exceptions;
using Gise.Ticket.Application.Features.Queries.Ticket.Request;
using Gise.Ticket.Application.Features.Queries.Ticket.Response;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Domain.Enums;
using MediatR;

namespace Gise.Ticket.Application.Features.Handlers.Ticket.QueryHandlers;

public sealed class GetTicketDownloadUrlQueryHandler : IRequestHandler<GetTicketDownloadUrlQuery, GetTicketDownloadUrlResponse>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketStorage _ticketStorage;

    public GetTicketDownloadUrlQueryHandler(ITicketRepository ticketRepository, ITicketStorage ticketStorage)
    {
        _ticketRepository = ticketRepository;
        _ticketStorage = ticketStorage;
    }

    public async Task<GetTicketDownloadUrlResponse> Handle(GetTicketDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken)
            ?? throw NotFoundException.Ticket(request.TicketId);

        if (!request.IsAdmin && ticket.CustomerId != request.CustomerId)
            throw ForbiddenException.NotTicketOwner(ticket.Id);

        // İptal edilen biletin PDF'i kapıda işe yaramaz; yanlışlıkla kullanılmasın diye verilmez.
        if (ticket.Status == TicketStatus.Cancelled)
            throw BusinessException.TicketCancelled(ticket.Id);

        var download = _ticketStorage.CreateDownloadUrl(ticket.PdfKey, $"gise-bilet-{ticket.Code}.pdf");
        return new GetTicketDownloadUrlResponse { Url = download.Url, ExpiresInSeconds = download.ExpiresInSeconds };
    }
}
