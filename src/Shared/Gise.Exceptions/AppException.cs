namespace Gise.Exceptions;

// Tüm uygulama hatalarının tabanı. Her hata bir HTTP durum kodu, makine okunabilir bir kod,
// kullanıcıya gösterilecek kısa bir mesaj ve ayrıntılı bir açıklama taşır.
// GlobalExceptionHandler bunları ProblemDetails'e çevirir (title, detail, errorCode).
public abstract class AppException : Exception
{
    public int StatusCode { get; }

    // Sabit kod, ör. "EVENT_NOT_FOUND". İstemci mesaja değil buna göre davranır.
    public string ErrorCode { get; }

    // Kısa, kullanıcıya gösterilebilir mesaj.
    public override string Message { get; }

    // Ayrıntılı açıklama.
    public string Description { get; }

    protected AppException(int statusCode, string errorCode, string message, string description)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Message = message;
        Description = description;
    }
}
