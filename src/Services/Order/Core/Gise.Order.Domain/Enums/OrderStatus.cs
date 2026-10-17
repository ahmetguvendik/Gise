namespace Gise.Order.Domain.Enums;

// Kullanıcının gördüğü durum. Saga'nın ara adımları (bilet bekleniyor, iade ediliyor) saga
// durumunda tutulur; sipariş bunlardan yalnızca sonucu yansıtır.
public enum OrderStatus
{
    Pending,
    AwaitingPayment,
    Completed,
    Cancelled,
    Expired
}
