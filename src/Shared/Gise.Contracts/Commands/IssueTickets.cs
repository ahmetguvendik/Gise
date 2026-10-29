namespace Gise.Contracts.Commands;

// Order → Ticket: ödeme alındı ve koltuklar satıldı, siparişin biletlerini çıkar (koltuk başına bir bilet).
// Cevap TicketsIssued ya da TicketIssueFailed.
public sealed class IssueTickets
{
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }

    // Keycloak'taki müşteri (sub); biletler onun adına çıkar.
    public string CustomerId { get; set; } = string.Empty;

    // Bilet e-postası için (Kafka TicketsIssued ile Notification'a geçer); biletlerde saklanmaz.
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    // Bilet ve PDF Catalog'a sormadan hazırlansın diye siparişten kopyalanır.
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public List<TicketSeat> Seats { get; set; } = [];
}

public sealed class TicketSeat
{
    public Guid SeatId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public decimal Price { get; set; }
}
