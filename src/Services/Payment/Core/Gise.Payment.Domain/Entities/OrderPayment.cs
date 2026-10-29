using Gise.Payment.Domain.Common;

namespace Gise.Payment.Domain.Entities;

// Siparişin ödeme özeti, sipariş başına tek satır; Id = Order'daki siparişin ID'si. Para çekilince ya da sipariş kapanınca
// (RefundPayment) yazılır. Ödeme ve iade aynı siparişte aynı anda işlenirse ikisi de bu satırı yazmaya çalışır, biri
// çakışma alıp yeniden denenir. Böylece sipariş kapandıktan sonra para çekilmez, çekilen para da iadesiz kalmaz.
public class OrderPayment : BaseEntity
{
    // Parası çekilen deneme.
    public Guid? PaidPaymentId { get; set; }

    // Sipariş kapandı (süre doldu ya da koltuklar satılamadı); bundan sonra gelen ödeme isteği para çekmez.
    public DateTime? ClosedAt { get; set; }
}
