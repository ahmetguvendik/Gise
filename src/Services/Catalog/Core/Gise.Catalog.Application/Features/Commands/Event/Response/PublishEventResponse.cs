namespace Gise.Catalog.Application.Features.Commands.Event.Response;

public sealed class PublishEventResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
