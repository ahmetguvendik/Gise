using Gise.Ticket.Application.Features.Commands.TicketTransfer.Response;
using MediatR;

namespace Gise.Ticket.Application.Features.Commands.TicketTransfer.Request;

// Alıcı aktarımı kabul eder: bilet ona geçer, yeni kod ve PDF verilir. Hepsi JWT'den.
public sealed class AcceptTicketTransferCommand : IRequest<AcceptTicketTransferResponse>
{
    public Guid TransferId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string Name { get; set; } = string.Empty;
}
