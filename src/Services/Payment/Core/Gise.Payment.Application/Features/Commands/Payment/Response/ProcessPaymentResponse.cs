using Gise.Payment.Domain.Enums;

namespace Gise.Payment.Application.Features.Commands.Payment.Response;

public sealed class ProcessPaymentResponse
{
    // Succeeded, Failed, Cancelled (sipariş kapanmıştı) ya da Pending (sağlayıcı cevap vermedi).
    public PaymentStatus Status { get; set; }
    public string? FailureReason { get; set; }
}
