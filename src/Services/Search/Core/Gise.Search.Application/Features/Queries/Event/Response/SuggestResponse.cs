namespace Gise.Search.Application.Features.Queries.Event.Response;

public sealed class SuggestResponse
{
    public List<SuggestedEvent> Events { get; set; } = [];

    // Tıklanınca bu adla aranır.
    public List<string> Venues { get; set; } = [];

    // Tıklanınca şehir filtresi olur.
    public List<string> Cities { get; set; } = [];
}

public sealed class SuggestedEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
