namespace Gise.Catalog.Application.Features.Commands.Event.Response;

public sealed class CancelEventResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
}
