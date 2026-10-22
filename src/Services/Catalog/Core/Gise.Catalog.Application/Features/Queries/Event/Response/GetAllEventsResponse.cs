namespace Gise.Catalog.Application.Features.Queries.Event.Response;

public sealed class GetAllEventsResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid VenueId { get; set; }
    public DateTime StartsAt { get; set; }

    // Afiş yoksa boş; ön yüz o zaman renkli yer tutucuyu çizer.
    public string? PosterUrl { get; set; }
    public string? PosterSrcset { get; set; }

    // "... ₺'den başlayan fiyatlarla" gösterimi için.
    public decimal? MinPrice { get; set; }
}
