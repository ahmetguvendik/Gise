namespace Gise.Contracts.Commands;

// Ticket → Order: biletler çıkarılamadı (etkinlik bu arada iptal edildi, koltuğun geçerli bir bileti zaten var).
// Sipariş iptal olur: ödeme iade edilir, satılan koltuklar geri açılır.
public sealed class TicketIssueFailed
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
