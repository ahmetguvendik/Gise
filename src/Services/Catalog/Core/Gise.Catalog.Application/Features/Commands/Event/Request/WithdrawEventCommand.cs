using MediatR;

namespace Gise.Catalog.Application.Features.Commands.Event.Request;

// Onay bekleyen etkinlik organizatörün değiştirebilmesi için taslağa döner.
public sealed class WithdrawEventCommand : IRequest
{
    public Guid EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
