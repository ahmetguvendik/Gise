using MediatR;

namespace Gise.Identity.Application.Features.Commands.User.Request;

// Admin: kullanıcıdan rol alır. Admin kendi admin rolünü alamaz.
public sealed class RevokeRoleCommand : IRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // İşlemi yapan admin (token'daki sub).
    public string RequestedBy { get; set; } = string.Empty;
}
