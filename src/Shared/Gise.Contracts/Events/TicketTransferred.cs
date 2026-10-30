namespace Gise.Contracts.Events;

// Ticket → ticket.events: aktarım kabul edildi; bilet artık alıcının, eski QR geçersiz. Notification bilet sahiplerini
// günceller (iptal ve erteleme e-postaları yeni sahibine gider) ve gönderene yazar.
public sealed class TicketTransferred
{
    public Guid TransferId { get; set; }
    public Guid TicketId { get; set; }
    // Bileti satın alanın siparişi; para iadesi yine bu siparişe yapılır.
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }

    public string FromCustomerId { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;

    // Kabul edenin token'ından.
    public string ToCustomerId { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;

    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
}
