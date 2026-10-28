namespace Gise.Order.Application.Sagas.Messages;

// Sipariş oluşturuldu: saga'yı başlatır. Siparişle aynı transaction'da outbox'a yazılır.
public sealed class OrderSubmitted
{
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public List<Guid> SeatIds { get; set; } = [];
}
