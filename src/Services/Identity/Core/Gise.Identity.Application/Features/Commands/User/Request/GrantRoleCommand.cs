using MediatR;

namespace Gise.Identity.Application.Features.Commands.User.Request;

// Admin: kullanıcıya rol verir (organizer, gate-staff, admin). Rol kullanıcının bir sonraki token'ında görünür.
public sealed class GrantRoleCommand : IRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // İşlemi yapan admin (token'daki sub).
    public string RequestedBy { get; set; } = string.Empty;
}
