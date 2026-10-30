namespace Gise.Ticket.Application.Features.Commands.TicketTransfer.Response;

public sealed class AcceptTicketTransferResponse
{
    public Guid TicketId { get; set; }

    // Yeni kod, okunaklı biçimde; eski kod artık geçersiz.
    public string Code { get; set; } = string.Empty;
}
