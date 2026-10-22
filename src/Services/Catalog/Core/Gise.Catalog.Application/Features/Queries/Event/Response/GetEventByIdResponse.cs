namespace Gise.Catalog.Application.Features.Queries.Event.Response;

public sealed class GetEventByIdResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid VenueId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string OrganizerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // İptal edildiyse dolu; sayfada "iptal edildi" uyarısı bununla gösterilir.
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public List<EventPriceResponse> Prices { get; set; } = [];
}

public sealed class EventPriceResponse
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
