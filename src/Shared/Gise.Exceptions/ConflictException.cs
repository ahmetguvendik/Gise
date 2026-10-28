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

    public static ConflictException SeatsUnavailable() => new(
        ErrorCodes.Hold.SeatsUnavailable,
        "Seçtiğiniz koltuklardan en az biri artık müsait değil.",
        "Koltuk başka biri tarafından tutuldu ya da satıldı; haritayı yenileyip yeniden seçin.");

    public static ConflictException HoldAlreadyActive(Guid eventId) => new(
        ErrorCodes.Hold.AlreadyActive,
        "Bu etkinlik için ödemesi bekleyen bir siparişiniz var.",
        $"'{eventId}' ID'li etkinlikte koltukları hâlâ tutulan bir siparişiniz var; yenisi için önce onu tamamlayın ya da vazgeçin.");
}
