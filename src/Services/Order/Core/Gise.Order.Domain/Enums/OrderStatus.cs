namespace Gise.Order.Domain.Enums;

// Kullanıcının gördüğü durum. Saga'nın ara adımları (koltuklar satılıyor, bilet bekleniyor) saga
// durumunda tutulur; sipariş bunlardan yalnızca sonucu yansıtır.
public enum OrderStatus
{
    Pending,
    AwaitingPayment,

    // Müşteri "Öde" dedi; ödemenin, koltuk satışının ve biletlerin sonucu bekleniyor.
    PaymentProcessing,
    Completed,
    Cancelled,
    Expired,

    // Tamamlanmış siparişin etkinliği iptal edildi; para iade edildi (bkz. tasarım 4.4).
    Refunded
}
