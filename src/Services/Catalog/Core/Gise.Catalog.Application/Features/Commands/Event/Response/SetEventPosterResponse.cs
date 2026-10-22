namespace Gise.Catalog.Application.Features.Commands.Event.Response;

public sealed class SetEventPosterResponse
{
    public Guid Id { get; set; }
    public string? PosterUrl { get; set; }
    public string? PosterSrcset { get; set; }
}
