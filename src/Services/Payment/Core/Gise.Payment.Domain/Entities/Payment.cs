using Gise.Payment.Domain.Common;
using Gise.Payment.Domain.Enums;

namespace Gise.Payment.Domain.Entities;

// Bir ödeme denemesi. Id, Order'ın denemeye verdiği kimlik (ProcessPayment.PaymentId): aynı komut iki kez gelse de para
// bir kez çekilir. Kart reddedilirse müşteri süre dolana kadar yeniden dener; sipariş başına birden çok deneme olur,
// parası çekilen en fazla bir tane (tekil indeks).
public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Test sağlayıcı sonucu kartın son dört hanesine göre belirler (bkz. tasarım 3.2).
    public string CardLast4 { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}
