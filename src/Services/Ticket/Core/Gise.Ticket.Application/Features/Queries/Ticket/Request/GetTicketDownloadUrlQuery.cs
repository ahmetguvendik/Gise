using Gise.Ticket.Application.Features.Queries.Ticket.Response;
using MediatR;

namespace Gise.Ticket.Application.Features.Queries.Ticket.Request;

// Biletin PDF'i için kısa süreli indirme adresi; yalnızca sahibi ve admin.
public sealed class GetTicketDownloadUrlQuery : IRequest<GetTicketDownloadUrlResponse>
{
    public Guid TicketId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
