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

    public static NotFoundException Order(Guid id) => new(
        ErrorCodes.Order.NotFound,
        "Sipariş bulunamadı.",
        $"'{id}' ID'li sipariş sistemde yok.");

    public static NotFoundException Ticket(Guid id) => new(
        ErrorCodes.Ticket.NotFound,
        "Bilet bulunamadı.",
        $"'{id}' ID'li bilet sistemde yok.");

    // Kapıda okutulan kod; açıklamada kodun kendisi yer almaz.
    public static NotFoundException TicketCode() => new(
        ErrorCodes.Ticket.NotFound,
        "Bilet bulunamadı.",
        "Bu kodla çıkmış bir bilet yok; kodu kontrol edip yeniden okutun.");

    public static NotFoundException EventStock(Guid eventId) => new(
        ErrorCodes.Stock.NotFound,
        "Etkinliğin koltuk bilgisi bulunamadı.",
        $"'{eventId}' ID'li etkinlik satışa açılmamış ya da koltukları henüz hazırlanıyor.");

    public static NotFoundException TicketTransfer(Guid id) => new(
        ErrorCodes.Ticket.TransferNotFound,
        "Aktarım bulunamadı.",
        $"'{id}' ID'li aktarım sistemde yok.");

    public static NotFoundException PendingTicketTransfer(Guid ticketId) => new(
        ErrorCodes.Ticket.TransferNotFound,
        "Bu biletin bekleyen bir aktarımı yok.",
        $"'{ticketId}' ID'li biletin geri alınacak bir aktarımı bulunmuyor.");
}
