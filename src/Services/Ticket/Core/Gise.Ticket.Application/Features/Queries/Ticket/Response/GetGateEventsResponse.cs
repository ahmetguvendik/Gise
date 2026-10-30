namespace Gise.Ticket.Application.Features.Queries.Ticket.Response;

public sealed class GetGateEventsResponse
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    // Okutmanın açık olduğu aralık.
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }

    // İptal edilmemiş biletler ve bunlardan okutulanlar.
    public int TicketCount { get; set; }
    public int CheckedInCount { get; set; }
}
