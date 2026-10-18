namespace Gise.Catalog.Application.Features.Commands.Venue.Response;

public sealed class UpdateVenueResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
