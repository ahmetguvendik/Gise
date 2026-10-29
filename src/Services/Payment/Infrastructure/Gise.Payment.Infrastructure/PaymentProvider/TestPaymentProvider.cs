using System.Collections.Concurrent;
using Gise.Exceptions;
using Gise.Payment.Application.Interfaces;

namespace Gise.Payment.Infrastructure.PaymentProvider;

// Gerçek sağlayıcıya bağlanmaz; sonucu kartın son dört hanesi belirler (bkz. tasarım 3.2). Diğer sonlar başarılı sayılır.
public sealed class TestPaymentProvider : IPaymentProvider
{
    // 0009: her denemenin ilk çağrısı hata verir. Yeniden deneme aynı process'te yapıldığı için (MassTransit retry)
    // denemeleri bellekte tutmak yeter.
    private readonly ConcurrentDictionary<Guid, byte> _failedOnce = new();

    public Task<PaymentProviderResult> ChargeAsync(Guid paymentId, decimal amount, string cardLast4, CancellationToken cancellationToken)
        => cardLast4 switch
        {
            "0002" => Task.FromResult(new PaymentProviderResult(PaymentProviderOutcome.Declined, "Kart reddedildi: yetersiz bakiye.")),
            "0005" => Task.FromResult(new PaymentProviderResult(PaymentProviderOutcome.NoResponse)),
            "0009" when _failedOnce.TryAdd(paymentId, 0) => throw ServiceUnavailableException.PaymentProvider(),
            _ => Task.FromResult(new PaymentProviderResult(PaymentProviderOutcome.Approved))
        };

    public Task RefundAsync(Guid paymentId, decimal amount, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
