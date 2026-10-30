using MediatR;

namespace Gise.Ticket.Application.Features.Commands.TicketTransfer.Request;

// Alıcı aktarımı reddeder; bilet gönderende kalır.
public sealed class DeclineTicketTransferCommand : IRequest
{
    public Guid TransferId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
}
