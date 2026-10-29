using Gise.Order.Domain.Common;
using Gise.Order.Domain.Enums;

namespace Gise.Order.Domain.Entities;

// Müşterinin gördüğü sipariş. Durumunu satın alma saga'sı (OrderStateMachine) her adımda günceller.
public class Order : BaseEntity
{
    public string CustomerId { get; set; } = string.Empty;

    // Sipariş açılırken token'dan (email, name) kopyalanır; bilet e-postası bu adrese gider. Token'da yoksa boş kalır.
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public Guid EventId { get; set; }

    // Sipariş açılırken Catalog'dan kopyalanır; Siparişlerim Catalog'a sormadan listelenir.
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }

    // Ödeme bu zamana kadar gelmezse sipariş düşer; koltuklar tutulunca tutmanın bitişine eşitlenir.
    public DateTime ExpiresAt { get; set; }

    // İptal ya da zaman aşımında müşteriye gösterilen sebep; ödeme beklenirken son denemenin reddedilme sebebi.
    public string? FailureReason { get; set; }

    // Son ödeme denemesi; ön yüz "Öde"nin sonucunu (reddedildi mi) bununla tanır.
    public Guid? LastPaymentId { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
}
