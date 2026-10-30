namespace Gise.Search.Application.Features.Queries.Event.Response;

public sealed class SearchEventsResponse
{
    public List<SearchEventItem> Items { get; set; } = [];
    public long Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    // Filtre için: aramaya uyan etkinliklerin şehirleri ve sayıları (şehir filtresinden bağımsız).
    public List<CityFacet> Cities { get; set; } = [];
}

public sealed class SearchEventItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public string? PosterUrl { get; set; }
    public string? PosterSrcset { get; set; }
}

public sealed class CityFacet
{
    public string Name { get; set; } = string.Empty;
    public long Count { get; set; }
}
