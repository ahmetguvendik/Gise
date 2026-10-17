using Gise.Order.Domain.Common;
using Gise.Order.Domain.Enums;

namespace Gise.Order.Domain.Entities;

public class Order : BaseEntity
{
    public string CustomerId { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTime ExpiresAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
}
