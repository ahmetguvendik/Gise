namespace Gise.Ticket.Application.Interfaces;

// İptal edilen etkinlikleri ticket-inbox-worker yazar (Kafka'dan EventCancelled); burada yalnızca okunur.
public interface ICancelledEventRepository
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken);
}
