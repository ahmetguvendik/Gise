namespace Gise.Payment.Application.Interfaces;

// Ödeme sağlayıcısı. Şimdilik yalnızca test sağlayıcısı var (bkz. tasarım 3.2). Geçici hatada (sağlayıcıya ulaşılamadı)
// exception fırlatır; komut yeniden denenir. Deneme kimliği sağlayıcıya idempotency anahtarı olarak gider.
public interface IPaymentProvider
{
    Task<PaymentProviderResult> ChargeAsync(Guid paymentId, decimal amount, string cardLast4, CancellationToken cancellationToken);

    Task RefundAsync(Guid paymentId, decimal amount, CancellationToken cancellationToken);
}

public sealed record PaymentProviderResult(PaymentProviderOutcome Outcome, string? DeclineReason = null);

public enum PaymentProviderOutcome
{
    Approved,
    Declined,

    // Sağlayıcı cevap vermedi; para çekildi mi bilinmiyor.
    NoResponse
}
