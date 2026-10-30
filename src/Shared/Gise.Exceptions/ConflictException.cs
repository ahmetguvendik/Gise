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

    public static ConflictException TicketAlreadyUsed(DateTime checkedInAt) => new(
        ErrorCodes.Ticket.AlreadyUsed,
        "Bu bilet daha önce okutuldu.",
        $"Bilet {checkedInAt:yyyy-MM-dd HH:mm} (UTC) tarihinde kullanılmış; aynı bilet ikinci kez girişe izin vermez.");

    public static ConflictException TicketSeatTaken() => new(
        ErrorCodes.Ticket.SeatTaken,
        "Koltuklardan birinin geçerli bir bileti zaten var.",
        "Aynı koltuğa ikinci bir bilet çıkarılamaz.");

    public static ConflictException HoldAlreadyActive(Guid eventId) => new(
        ErrorCodes.Hold.AlreadyActive,
        "Bu etkinlik için ödemesi bekleyen bir siparişiniz var.",
        $"'{eventId}' ID'li etkinlikte koltukları hâlâ tutulan bir siparişiniz var; yenisi için önce onu tamamlayın ya da vazgeçin.");

    public static ConflictException ConcurrentUpdate() => new(
        ErrorCodes.General.ConcurrentUpdate,
        "Kayıt bu sırada başka bir işlemle değişti.",
        "Sayfayı yenileyip yeniden deneyin.");

    public static ConflictException TicketTransferPending(Guid ticketId) => new(
        ErrorCodes.Ticket.TransferPending,
        "Bu biletin bekleyen bir aktarımı var.",
        $"'{ticketId}' ID'li bileti yeniden aktarmak için önce bekleyen aktarımı geri alın.");

    public static ConflictException TicketTransferNotPending(Guid transferId) => new(
        ErrorCodes.Ticket.TransferNotPending,
        "Bu aktarım artık geçerli değil.",
        $"'{transferId}' ID'li aktarım geri alınmış, reddedilmiş, kabul edilmiş ya da süresi dolmuş.");
}
