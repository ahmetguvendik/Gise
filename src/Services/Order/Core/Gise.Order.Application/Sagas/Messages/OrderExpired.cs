namespace Gise.Order.Application.Sagas.Messages;

// Saga'nın planladığı zaman aşımı (Quartz): ödeme süresi doldu ya da koltuklar zamanında tutulamadı.
public sealed class OrderExpired
{
    public Guid OrderId { get; set; }
}
