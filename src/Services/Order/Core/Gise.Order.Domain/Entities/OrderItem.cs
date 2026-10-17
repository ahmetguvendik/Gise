using Gise.Order.Domain.Common;

namespace Gise.Order.Domain.Entities;

// Fiyat, sipariş anındaki fiyattır; Catalog'da fiyat sonradan değişse de sipariş etkilenmez.
public sealed class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid SeatId { get; private set; }
    public decimal Price { get; private set; }

    private OrderItem() { }

    internal static OrderItem Create(Guid orderId, Guid seatId, decimal price) => new()
    {
        OrderId = orderId,
        SeatId = seatId,
        Price = price
    };
}
