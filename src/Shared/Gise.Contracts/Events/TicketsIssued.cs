namespace Gise.Contracts.Events;

// Ticket → ticket.events: siparişin biletleri çıktı. Notification müşteriye e-posta gönderir ve etkinliğin bilet
// sahiplerini kendi tarafında tutar (iptal ve tarih değişikliği bildirimleri için). RabbitMQ'daki aynı adlı cevaptan
// (Gise.Contracts.Commands.TicketsIssued) ayrıdır: o yalnızca Order'a gider.
public sealed class TicketsIssued
{
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }
    public string CustomerId { get; set; } = string.Empty;

    // Sipariş açılırken token'dan alınır (bkz. tasarım 3.9); boşsa e-posta gönderilmez.
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public List<IssuedTicket> Tickets { get; set; } = [];
}

public sealed class IssuedTicket
{
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
}
