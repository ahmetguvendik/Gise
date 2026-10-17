using Gise.Payment.Domain.Common;
using Gise.Payment.Domain.Enums;

namespace Gise.Payment.Domain.Entities;

public class Payment : BaseEntity
{
    // Sipariş başına tek ödeme (tekil indeks): ProcessPayment iki kez gelse de para iki kez çekilmez.
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Test sağlayıcı sonucu kartın son dört hanesine göre belirler (bkz. tasarım 3.2).
    public string CardLast4 { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}
