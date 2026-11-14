namespace Gise.Exceptions;

// İsteğin bağlı olduğu başka bir servise şu an ulaşılamıyor; istek doğru, biraz sonra tekrar denenebilir. → 503
public class ServiceUnavailableException : AppException
{
    public ServiceUnavailableException(string errorCode, string message, string description)
        : base(503, errorCode, message, description)
    {
    }

    public static ServiceUnavailableException Catalog() => new(
        ErrorCodes.General.ServiceUnavailable,
        "Şu an sipariş alınamıyor; biraz sonra tekrar deneyin.",
        "Etkinlik bilgisi Catalog'dan alınamadı.");

    public static ServiceUnavailableException Keycloak(string description) => new(
        ErrorCodes.General.ServiceUnavailable,
        "Kimlik sistemine şu an ulaşılamıyor; biraz sonra tekrar deneyin.",
        description);

    public static ServiceUnavailableException Elasticsearch(string description) => new(
        ErrorCodes.General.ServiceUnavailable,
        "Denetim kayıtlarına şu an ulaşılamıyor; biraz sonra tekrar deneyin.",
        description);

    public static ServiceUnavailableException PaymentProvider() => new(
        ErrorCodes.General.ServiceUnavailable,
        "Ödeme sağlayıcısına şu an ulaşılamıyor.",
        "Sağlayıcı geçici bir hata verdi; ödeme yeniden denenecek.");
}
