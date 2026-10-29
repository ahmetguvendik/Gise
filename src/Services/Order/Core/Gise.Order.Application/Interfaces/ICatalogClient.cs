namespace Gise.Order.Application.Interfaces;

// Catalog'un gRPC ucu (senkron). Sipariş açılırken etkinliğin özeti alınır.
public interface ICatalogClient
{
    // Satıştaki etkinliğin özeti. Etkinlik yoksa NotFoundException, satışta değilse BusinessException,
    // Catalog'a ulaşılamazsa ServiceUnavailableException.
    Task<EventSummary> GetEventSummaryAsync(Guid eventId, CancellationToken cancellationToken);
}

public sealed record EventSummary(string Title, DateTime StartsAt, string VenueName, string City);
