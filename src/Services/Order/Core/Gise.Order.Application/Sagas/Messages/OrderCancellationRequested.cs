namespace Gise.Order.Application.Sagas.Messages;

// Müşteri ödeme beklenirken siparişten vazgeçti.
public sealed class OrderCancellationRequested
{
    public Guid OrderId { get; set; }
}
