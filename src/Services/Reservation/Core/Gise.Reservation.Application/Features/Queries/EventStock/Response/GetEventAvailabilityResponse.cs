namespace Gise.Reservation.Application.Features.Queries.EventStock.Response;

public sealed class GetEventAvailabilityResponse
{
    public Guid EventId { get; set; }
    public int SeatCount { get; set; }

    // Satılmamış koltuklar; sepette tutulanlar dahil, çünkü 10 dakika içinde geri açılabilirler.
    public int AvailableCount { get; set; }
}
