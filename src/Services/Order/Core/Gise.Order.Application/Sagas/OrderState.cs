using MassTransit;

namespace Gise.Order.Application.Sagas;

// Saga'nın kendi durumu: hangi adımda olduğu ve planlanan zaman aşımı. Siparişin verisi orders tablosunda.
public sealed class OrderState : SagaStateMachineInstance
{
    // Siparişin ID'si.
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    // Planlanan zaman aşımı mesajı; yeniden planlanınca ya da sipariş kapanınca iptal edilir.
    public Guid? ExpiryTokenId { get; set; }
}
