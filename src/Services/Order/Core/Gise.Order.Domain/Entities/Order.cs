using Gise.Order.Domain.Common;
using Gise.Order.Domain.Enums;
using Gise.Order.Domain.Exceptions;

namespace Gise.Order.Domain.Entities;

public sealed class Order : BaseEntity
{
    public const int MaxSeatsPerOrder = 10;

    private readonly List<OrderItem> _items = [];

    public string CustomerId { get; private set; } = string.Empty;
    public Guid EventId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;

    private Order() { }

    public static Order Create(string customerId, Guid eventId, IReadOnlyCollection<(Guid SeatId, decimal Price)> seats, DateTime expiresAt)
    {
        if (seats.Count is 0 or > MaxSeatsPerOrder)
            throw new DomainException($"Bir siparişte en az 1, en fazla {MaxSeatsPerOrder} koltuk olabilir.");

        if (seats.Select(s => s.SeatId).Distinct().Count() != seats.Count)
            throw new DomainException("Aynı koltuk bir siparişte iki kez yer alamaz.");

        var order = new Order
        {
            CustomerId = customerId,
            EventId = eventId,
            Status = OrderStatus.Pending,
            ExpiresAt = expiresAt
        };

        order._items.AddRange(seats.Select(s => OrderItem.Create(order.Id, s.SeatId, s.Price)));
        order.TotalAmount = order._items.Sum(i => i.Price);

        return order;
    }
}
