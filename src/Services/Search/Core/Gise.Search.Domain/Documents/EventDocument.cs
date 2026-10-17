namespace Gise.Search.Domain.Documents;

// Elasticsearch'teki arama dokümanı; EF entity'si değil. Catalog olaylarından (inbox üzerinden) beslenir.
public sealed class EventDocument
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public decimal? MinPrice { get; set; }
}
