namespace Gise.Exceptions;

// İstenen kayıt yok. → 404
public class NotFoundException : AppException
{
    public NotFoundException(string errorCode, string message, string description)
        : base(404, errorCode, message, description)
    {
    }

    public static NotFoundException Venue(Guid id) => new(
        ErrorCodes.Venue.NotFound,
        "Mekân bulunamadı.",
        $"'{id}' ID'li mekân sistemde yok.");

    public static NotFoundException Event(Guid id) => new(
        ErrorCodes.Event.NotFound,
        "Etkinlik bulunamadı.",
        $"'{id}' ID'li etkinlik sistemde yok.");

    public static NotFoundException EventStock(Guid eventId) => new(
        ErrorCodes.Stock.NotFound,
        "Etkinliğin koltuk bilgisi bulunamadı.",
        $"'{eventId}' ID'li etkinlik satışa açılmamış ya da koltukları henüz hazırlanıyor.");
}
