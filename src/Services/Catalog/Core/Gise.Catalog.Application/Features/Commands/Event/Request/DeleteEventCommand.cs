using MediatR;

namespace Gise.Catalog.Application.Features.Commands.Event.Request;

// Yalnızca taslak silinir: kimse görmediği için iz bırakmaya gerek yok. Yayındaki etkinlik iptal edilir.
public sealed class DeleteEventCommand : IRequest
{
    public Guid EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
