namespace Gise.Order.Application.Sagas.Messages;

// Müşteri ödeme beklenirken "Öde" dedi. Kartın tam numarası order-api'de kalır; saga'ya son dört hanesi gelir.
public sealed class OrderPaymentRequested
{
    public Guid OrderId { get; set; }

    // Bu denemenin kimliği; Payment'ta idempotency anahtarı.
    public Guid PaymentId { get; set; }
    public string CardLast4 { get; set; } = string.Empty;
}
