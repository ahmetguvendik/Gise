using Gise.Ticket.Application.Features.Queries.Ticket.Response;
using MediatR;

namespace Gise.Ticket.Application.Features.Queries.Ticket.Request;

// Müşterinin biletleri; OrderId verilirse yalnızca o siparişinkiler (sipariş sayfası).
public sealed class GetMyTicketsQuery : IRequest<IReadOnlyList<GetMyTicketsResponse>>
{
    public string CustomerId { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
}
