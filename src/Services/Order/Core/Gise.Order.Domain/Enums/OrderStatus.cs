namespace Gise.Order.Domain.Enums;

// Kullanıcının gördüğü durum. Saga'nın ara adımları (koltuklar satılıyor, bilet bekleniyor) saga
// durumunda tutulur; sipariş bunlardan yalnızca sonucu yansıtır.
public enum OrderStatus
{
    Pending,
    AwaitingPayment,

    // Müşteri "Öde" dedi; ödemenin ve koltuk satışının sonucu bekleniyor.
    PaymentProcessing,
    Completed,
    Cancelled,
    Expired
}
