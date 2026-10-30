using MediatR;

namespace Gise.Ticket.Application.Features.Commands.TicketTransfer.Request;

// Gönderen, alıcı kabul etmeden önce aktarımı geri alır.
public sealed class CancelTicketTransferCommand : IRequest
{
    public Guid TicketId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
}
