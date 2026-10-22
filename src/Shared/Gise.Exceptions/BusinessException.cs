namespace Gise.Exceptions;

// İstek biçim olarak doğru ama iş kuralına aykırı. → 422
public class BusinessException : AppException
{
    public BusinessException(string errorCode, string message, string description)
        : base(422, errorCode, message, description)
    {
    }

    public static BusinessException VenueHasEvents(Guid venueId) => new(
        ErrorCodes.Venue.HasEvents,
        "Bu mekânda etkinlik olduğu için bölüm düzeni değiştirilemez ve mekân silinemez.",
        $"'{venueId}' ID'li mekânda en az bir etkinlik (taslak dahil) var.");

    public static BusinessException EventCancelled(Guid eventId) => new(
        ErrorCodes.Event.Cancelled,
        "İptal edilmiş etkinlikte değişiklik yapılamaz.",
        $"'{eventId}' ID'li etkinlik iptal edilmiş.");

    public static BusinessException EventStarted(Guid eventId) => new(
        ErrorCodes.Event.AlreadyStarted,
        "Başlamış ya da geçmiş etkinlikte değişiklik yapılamaz.",
        $"'{eventId}' ID'li etkinliğin başlangıç zamanı geçmiş.");

    public static BusinessException OnlyDraftCanBeDeleted(Guid eventId) => new(
        ErrorCodes.Event.NotDraft,
        "Yalnızca taslak etkinlik silinebilir; yayındaki etkinlik iptal edilir.",
        $"'{eventId}' ID'li etkinlik taslak değil.");

    public static BusinessException DraftCannotBeCancelled(Guid eventId) => new(
        ErrorCodes.Event.NotPublished,
        "Taslak etkinlik iptal edilmez, silinir.",
        $"'{eventId}' ID'li etkinlik henüz yayınlanmamış.");

    public static BusinessException VenueLocked(Guid eventId) => new(
        ErrorCodes.Event.VenueLocked,
        "Yayındaki etkinliğin mekânı değiştirilemez.",
        $"'{eventId}' ID'li etkinliğin koltuk stoğu mekânın koltuklarıyla kuruldu.");

    public static BusinessException EventNotPublished(Guid eventId) => new(
        ErrorCodes.Event.NotPublished,
        "Etkinlik yayında değil.",
        $"'{eventId}' ID'li etkinlik satışa açık değil; koltukları yalnızca yayındaki etkinlik için verilir.");

    public static BusinessException EventAlreadyStarted(Guid eventId) => new(
        ErrorCodes.Event.AlreadyStarted,
        "Başlamış ya da geçmiş etkinlik yayınlanamaz.",
        $"'{eventId}' ID'li etkinliğin başlangıç zamanı geçmiş.");

    public static BusinessException MissingSectionPrices(IEnumerable<string> sectionNames) => new(
        ErrorCodes.Price.MissingSections,
        "Mekânın her bölümünün fiyatı girilmelidir.",
        $"Fiyatı olmayan bölümler: {string.Join(", ", sectionNames)}.");

    public static BusinessException SectionNotInVenue(Guid sectionId) => new(
        ErrorCodes.Price.SectionNotInVenue,
        "Fiyat girilen bölümlerden biri bu etkinliğin mekânına ait değil.",
        $"'{sectionId}' ID'li bölüm etkinliğin mekânında yok.");
}
