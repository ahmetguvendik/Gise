using Gise.Ticket.Application.Features.Queries.TicketTransfer.Response;
using MediatR;

namespace Gise.Ticket.Application.Features.Queries.TicketTransfer.Request;

// Giriş yapanın e-postasına gönderilmiş, hâlâ kabul edilebilir aktarımlar. E-posta doğrulanmamışsa liste boş.
public sealed class GetIncomingTransfersQuery : IRequest<IReadOnlyList<GetIncomingTransfersResponse>>
{
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
}
