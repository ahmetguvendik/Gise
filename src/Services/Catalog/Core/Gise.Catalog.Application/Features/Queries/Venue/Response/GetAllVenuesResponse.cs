namespace Gise.Catalog.Application.Features.Queries.Venue.Response;

public sealed class GetAllVenuesResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
