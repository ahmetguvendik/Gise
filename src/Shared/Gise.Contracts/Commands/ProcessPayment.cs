namespace Gise.Contracts.Commands;

// Order → Payment: müşteri "Öde" dedi, bu denemenin parasını çek. Cevap PaymentSucceeded ya da PaymentFailed;
// sağlayıcı hiç cevap vermezse (test kartı 0005) cevap gelmez, saga'nın zaman aşımı devreye girer.
public sealed class ProcessPayment
{
    // Denemenin kimliği: aynı komut iki kez gelse de para bir kez çekilir.
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }

    // Kartın tam numarası sisteme girmez; test sağlayıcı sonucu son dört haneye göre belirler (bkz. tasarım 3.2).
    public string CardLast4 { get; set; } = string.Empty;
}
