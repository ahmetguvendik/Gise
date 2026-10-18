namespace Gise.Catalog.Application.Features.Queries.Event.Response;

public sealed class GetEventSeatsResponse
{
    public Guid EventId { get; set; }
    public List<EventSeatResponse> Seats { get; set; } = [];
}

public sealed class EventSeatResponse
{
    public Guid SeatId { get; set; }
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public decimal Price { get; set; }
}
