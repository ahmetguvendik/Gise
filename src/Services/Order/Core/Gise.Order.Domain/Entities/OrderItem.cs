using Gise.Order.Domain.Common;

namespace Gise.Order.Domain.Entities;

// Koltuk ve fiyat, tutma anındaki hâliyle kopyalanır; Catalog'da fiyat sonradan değişse de sipariş etkilenmez.
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid SeatId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public decimal Price { get; set; }
}
