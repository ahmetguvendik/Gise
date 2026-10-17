namespace Gise.Exceptions;

// Kayıt zaten var ya da başka bir işlemle çakışıyor. → 409
public class ConflictException : AppException
{
    public ConflictException(string errorCode, string message, string description)
        : base(409, errorCode, message, description)
    {
    }

    public static ConflictException EventAlreadyPublished(Guid eventId) => new(
        ErrorCodes.Event.AlreadyPublished,
        "Etkinlik zaten yayında.",
        $"'{eventId}' ID'li etkinlik daha önce yayınlanmış.");
}
