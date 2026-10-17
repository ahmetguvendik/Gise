using Gise.Order.Domain.Common;

namespace Gise.Order.Domain.Entities;

// Fiyat, sipariş anındaki fiyattır; Catalog'da fiyat sonradan değişse de sipariş etkilenmez.
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid SeatId { get; set; }
    public decimal Price { get; set; }
}
