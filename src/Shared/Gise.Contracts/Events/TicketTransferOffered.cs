namespace Gise.Contracts.Events;

// Ticket → ticket.events: bilet sahibi biletini bir e-posta adresine aktarmak istedi. Notification alıcıya yazar; alıcı o
// adresle giriş yapıp kabul ederse TicketTransferred gelir.
public sealed class TicketTransferOffered
{
    public Guid TransferId { get; set; }
    public Guid TicketId { get; set; }
    public Guid EventId { get; set; }
    public string FromCustomerId { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;

    // Alıcının e-postası (küçük harf). Hesabı olmayabilir; kabul etmek için bu adresle kaydolur.
    public string ToEmail { get; set; } = string.Empty;

    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }

    // Bu andan sonra kabul edilemez (etkinlikten belli bir süre önce).
    public DateTime ExpiresAt { get; set; }
}
