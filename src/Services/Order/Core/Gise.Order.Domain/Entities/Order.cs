using Gise.Order.Domain.Common;
using Gise.Order.Domain.Enums;

namespace Gise.Order.Domain.Entities;

// Müşterinin gördüğü sipariş. Durumunu satın alma saga'sı (OrderStateMachine) her adımda günceller.
public class Order : BaseEntity
{
    public string CustomerId { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }

    // Ödeme bu zamana kadar gelmezse sipariş düşer; koltuklar tutulunca tutmanın bitişine eşitlenir.
    public DateTime ExpiresAt { get; set; }

    // İptal ya da zaman aşımında müşteriye gösterilen sebep.
    public string? FailureReason { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
}
