namespace Gise.Ticket.Application.Features.Queries.TicketTransfer.Response;

public sealed class GetIncomingTransfersResponse
{
    public Guid TransferId { get; set; }
    public Guid TicketId { get; set; }
    public string FromName { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }

    // Bu andan sonra kabul edilemez.
    public DateTime ExpiresAt { get; set; }
}
