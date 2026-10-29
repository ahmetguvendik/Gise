using MassTransit;

namespace Gise.Order.Application.Sagas;

// Saga'nın kendi durumu: hangi adımda olduğu, planlanan zaman aşımı ve sonraki adımların ihtiyaç duyduğu bilgiler.
// Siparişin verisi orders tablosunda.
public sealed class OrderState : SagaStateMachineInstance
{
    // Siparişin ID'si.
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    // Koltukları bırakırken Reservation'a gönderilir.
    public Guid EventId { get; set; }
    public string CustomerId { get; set; } = string.Empty;

    // Koltuklar tutulunca Reservation'ın bildirdiği toplam; Payment'a bu tutar gider.
    public decimal Amount { get; set; }

    // Süren ödeme denemesi.
    public Guid? PaymentId { get; set; }

    // Planlanan zaman aşımı mesajı; yeniden planlanınca ya da sipariş kapanınca iptal edilir.
    public Guid? ExpiryTokenId { get; set; }
}
