namespace Gise.Payment.Domain.Enums;

public enum PaymentStatus
{
    // Sağlayıcıdan cevap bekleniyor (test kartı 0005'te hiç gelmez).
    Pending,
    Succeeded,
    Failed,
    Refunded,

    // Para çekilmeden sipariş kapandı.
    Cancelled
}
