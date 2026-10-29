namespace Gise.Contracts.Commands;

// Payment → Order: ödeme alınamadı (kart reddedildi…). Sipariş açık kalır; müşteri süre dolana kadar başka kartla dener.
public sealed class PaymentFailed
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }

    // Müşteriye gösterilecek sebep, ör. "Kart reddedildi: yetersiz bakiye."
    public string Reason { get; set; } = string.Empty;
}
