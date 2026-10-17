namespace Gise.Catalog.Application.Features.Commands.Venue.Response;

public sealed class CreateVenueResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int SectionCount { get; set; }
    public int SeatCount { get; set; }
}
