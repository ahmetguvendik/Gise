namespace Gise.Contracts.Commands;

// Order → Reservation: siparişin tuttuğu koltukları bırak (müşteri vazgeçti ya da ödeme süresi doldu).
// Cevap beklenmez; tutma yoksa ya da zaten bırakılmışsa zararsızdır.
public sealed class ReleaseSeats
{
    public Guid OrderId { get; set; }
}
