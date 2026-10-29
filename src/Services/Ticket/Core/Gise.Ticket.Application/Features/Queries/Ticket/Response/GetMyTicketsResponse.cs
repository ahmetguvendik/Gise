namespace Gise.Ticket.Application.Features.Queries.Ticket.Response;

public sealed class GetMyTicketsResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public decimal Price { get; set; }

    // Okunaklı biçimde: "ABCD-EFGH-JKMN-PQRS".
    public string Code { get; set; } = string.Empty;

    // Valid, Used, Cancelled.
    public string Status { get; set; } = string.Empty;
    public DateTime? CheckedInAt { get; set; }
}
