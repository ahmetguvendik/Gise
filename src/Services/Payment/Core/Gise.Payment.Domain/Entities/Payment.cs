using Gise.Payment.Domain.Common;
using Gise.Payment.Domain.Enums;
using Gise.Payment.Domain.Exceptions;

namespace Gise.Payment.Domain.Entities;

public sealed class Payment : BaseEntity
{
    public const int FailureReasonMaxLength = 500;

    // Sipariş başına tek ödeme (tekil indeks): ProcessPayment iki kez gelse de para iki kez çekilmez.
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }

    // Test sağlayıcı sonucu kartın son dört hanesine göre belirler (bkz. tasarım 3.2).
    public string CardLast4 { get; private set; } = string.Empty;
    public string? FailureReason { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    private Payment() { }

    public static Payment Create(Guid orderId, decimal amount, string cardLast4)
    {
        if (amount <= 0)
            throw new DomainException("Ödeme tutarı sıfırdan büyük olmalıdır.");

        if (cardLast4.Length != 4 || !cardLast4.All(char.IsAsciiDigit))
            throw new DomainException("Kartın son dört hanesi 4 rakamdan oluşmalıdır.");

        return new Payment
        {
            OrderId = orderId,
            Amount = amount,
            CardLast4 = cardLast4,
            Status = PaymentStatus.Pending
        };
    }
}
