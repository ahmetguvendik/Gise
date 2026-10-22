namespace Gise.Exceptions;

// Rol yeterli ama kayıt kullanıcıya ait değil (ör. başka organizatörün etkinliği). → 403
public class ForbiddenException : AppException
{
    public ForbiddenException(string errorCode, string message, string description)
        : base(403, errorCode, message, description)
    {
    }

    public static ForbiddenException NotEventOwner(Guid eventId) => new(
        ErrorCodes.Event.NotOwner,
        "Yalnızca kendi etkinliğinizde değişiklik yapabilirsiniz.",
        $"'{eventId}' ID'li etkinlik başka bir organizatöre ait.");

    public static ForbiddenException NotHoldOwner(Guid holdId) => new(
        ErrorCodes.Hold.NotOwner,
        "Yalnızca kendi tuttuğunuz koltukları görebilir ve bırakabilirsiniz.",
        $"'{holdId}' ID'li tutma başka bir kullanıcıya ait.");

    public static ForbiddenException PublishedEventAdminOnly(Guid eventId) => new(
        ErrorCodes.Event.AdminOnly,
        "Yayındaki etkinlikte değişikliği yalnızca admin yapabilir.",
        $"'{eventId}' ID'li etkinlik yayında.");
}
