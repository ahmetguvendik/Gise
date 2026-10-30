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

    public static ForbiddenException NotOrderOwner(Guid orderId) => new(
        ErrorCodes.Order.NotOwner,
        "Yalnızca kendi siparişinizi görebilir ve değiştirebilirsiniz.",
        $"'{orderId}' ID'li sipariş başka bir müşteriye ait.");

    public static ForbiddenException NotTicketOwner(Guid ticketId) => new(
        ErrorCodes.Ticket.NotOwner,
        "Bu bilet size ait değil.",
        $"'{ticketId}' ID'li bileti yalnızca sahibi görebilir.");

    public static ForbiddenException PublishedEventAdminOnly(Guid eventId) => new(
        ErrorCodes.Event.AdminOnly,
        "Yayındaki etkinlikte değişikliği yalnızca admin yapabilir.",
        $"'{eventId}' ID'li etkinlik yayında.");

    public static ForbiddenException EmailNotVerified() => new(
        ErrorCodes.Auth.EmailNotVerified,
        "E-posta adresiniz doğrulanmamış.",
        "Size aktarılan biletleri görmek ve kabul etmek için e-posta adresinizi doğrulayın.");

    public static ForbiddenException NotTransferRecipient(Guid transferId) => new(
        ErrorCodes.Ticket.TransferNotRecipient,
        "Bu aktarım size gönderilmedi.",
        $"'{transferId}' ID'li aktarımı yalnızca alıcı olarak yazılan e-postayla giriş yapan kişi kabul ya da reddedebilir.");
}
