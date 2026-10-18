namespace Gise.Catalog.Application.Features.Queries.Venue.Response;

// Koltuklar tek tek değil, sıra başına sayı olarak döner; bir mekânda binlerce koltuk olabilir.
public sealed class GetVenueByIdResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    // Etkinliği olan mekânın düzeni değişmez, mekân silinemez.
    public bool HasEvents { get; set; }
    public List<VenueSectionResponse> Sections { get; set; } = [];
}

public sealed class VenueSectionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<VenueRowResponse> Rows { get; set; } = [];
}

public sealed class VenueRowResponse
{
    public string Row { get; set; } = string.Empty;
    public int SeatCount { get; set; }
}
