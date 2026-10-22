using MediatR;

namespace Gise.Catalog.Application.Features.Commands.Event.Request;

// Organizatör taslağı admin onayına gönderir; onay beklerken değiştiremez.
public sealed class SubmitEventCommand : IRequest
{
    public Guid EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
