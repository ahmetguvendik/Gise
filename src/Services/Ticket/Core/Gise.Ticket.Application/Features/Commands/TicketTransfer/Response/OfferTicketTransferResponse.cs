namespace Gise.Ticket.Application.Features.Commands.TicketTransfer.Response;

public sealed class OfferTicketTransferResponse
{
    public Guid TransferId { get; set; }
    public string ToEmail { get; set; } = string.Empty;

    // Bu andan sonra kabul edilemez.
    public DateTime ExpiresAt { get; set; }
}
