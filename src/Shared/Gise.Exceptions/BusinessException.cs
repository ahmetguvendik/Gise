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

    public static BusinessException EventNotDraft(Guid eventId) => new(
        ErrorCodes.Event.NotDraft,
        "Bu işlem yalnızca taslak etkinlikte yapılabilir.",
        $"'{eventId}' ID'li etkinlik taslak değil.");

    public static BusinessException EventInReview(Guid eventId) => new(
        ErrorCodes.Event.InReview,
        "Onay bekleyen etkinlik değiştirilemez; önce onaydan geri çekin.",
        $"'{eventId}' ID'li etkinlik admin onayı bekliyor.");

    public static BusinessException EventNotInReview(Guid eventId) => new(
        ErrorCodes.Event.NotInReview,
        "Etkinlik onay beklemiyor.",
        $"'{eventId}' ID'li etkinlik onaya gönderilmemiş.");

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

    public static BusinessException MediaTypeNotAllowed(IEnumerable<string> allowedContentTypes) => new(
        ErrorCodes.Media.TypeNotAllowed,
        "Bu dosya türü yüklenemez.",
        $"İzin verilen türler: {string.Join(", ", allowedContentTypes)}.");

    public static BusinessException MediaTooLarge(long maxSizeBytes) => new(
        ErrorCodes.Media.TooLarge,
        "Dosya boyutu sınırı aşıyor.",
        $"En fazla {maxSizeBytes / (1024 * 1024)} MB yükleyebilirsiniz.");

    public static BusinessException MediaTooManyPixels(int maxMegapixels) => new(
        ErrorCodes.Media.TooLarge,
        "Görselin çözünürlüğü çok yüksek.",
        $"En fazla {maxMegapixels} megapiksel olabilir.");

    public static BusinessException MediaNotFound() => new(
        ErrorCodes.Media.NotFound,
        "Yüklenen dosya bulunamadı.",
        "Yükleme tamamlanmamış ya da geçici dosyanın süresi dolmuş olabilir.");

    public static BusinessException MediaCorruptContent() => new(
        ErrorCodes.Media.CorruptContent,
        "Dosya içeriği türüyle uyuşmuyor.",
        "Dosya bozuk olabilir ya da uzantısı gerçek içeriğini yansıtmıyor.");

    public static BusinessException EventNotOnSale(Guid eventId) => new(
        ErrorCodes.Stock.NotOnSale,
        "Bu etkinlik satışta değil.",
        $"'{eventId}' ID'li etkinlik iptal edilmiş ya da başlamış.");

    public static BusinessException SeatsNotInEvent(IEnumerable<Guid> seatIds) => new(
        ErrorCodes.Hold.SeatNotInEvent,
        "Seçilen koltuklardan biri bu etkinliğe ait değil.",
        $"Etkinlikte olmayan koltuklar: {string.Join(", ", seatIds)}.");

    public static BusinessException OrderNotCancellable(Guid orderId) => new(
        ErrorCodes.Order.NotCancellable,
        "Bu siparişten şu an vazgeçilemez.",
        $"'{orderId}' ID'li siparişin koltukları henüz tutulmadı ya da sipariş kapandı.");

    public static BusinessException OrderNotPayable(Guid orderId) => new(
        ErrorCodes.Order.NotPayable,
        "Bu sipariş şu an ödenemez.",
        $"'{orderId}' ID'li siparişin koltukları henüz tutulmadı, ödemesi sürüyor ya da sipariş kapandı.");

    public static BusinessException HoldNotActive(Guid orderId) => new(
        ErrorCodes.Hold.NotActive,
        "Koltukların tutulması sona erdiği için satış tamamlanamadı.",
        $"'{orderId}' ID'li siparişin koltukları bırakılmış (süre doldu ya da etkinlik iptal edildi).");

    public static BusinessException HoldOrderClosed(Guid orderId) => new(
        ErrorCodes.Hold.OrderClosed,
        "Sipariş kapandığı için koltuklar tutulmadı.",
        $"'{orderId}' ID'li siparişin koltukları daha önce bırakıldı (vazgeçildi ya da süresi doldu).");
}
