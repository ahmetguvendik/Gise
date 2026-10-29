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
}
