namespace Gise.Contracts.Commands;

// Order → Payment: sipariş kapandı (süre doldu ya da koltuklar satılamadı). Siparişin çekilmiş parası iade edilir,
// süren deneme iptal edilir; bundan sonra gelen ödeme isteği para çekmez. Cevap beklenmez.
public sealed class RefundPayment
{
    public Guid OrderId { get; set; }
}
