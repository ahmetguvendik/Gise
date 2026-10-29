namespace Gise.Contracts.Commands;

// Order → Reservation: siparişin tuttuğu koltukları bırak (müşteri vazgeçti, ödeme süresi doldu); bilet çıkarılamadıysa
// satılmış koltuklar da geri açılır. Cevap beklenmez; tutma yoksa ya da zaten bırakılmışsa zararsızdır.
public sealed class ReleaseSeats
{
    public Guid OrderId { get; set; }

    // Tutma henüz yoksa Reservation siparişi "bırakıldı" olarak kaydeder; kayıt bu bilgilerle yazılır.
    public Guid EventId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
}
