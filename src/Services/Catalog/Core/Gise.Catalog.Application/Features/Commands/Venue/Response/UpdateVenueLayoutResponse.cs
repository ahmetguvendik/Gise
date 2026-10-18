namespace Gise.Catalog.Application.Features.Commands.Venue.Response;

public sealed class UpdateVenueLayoutResponse
{
    public Guid Id { get; set; }
    public int SectionCount { get; set; }
    public int SeatCount { get; set; }
}
