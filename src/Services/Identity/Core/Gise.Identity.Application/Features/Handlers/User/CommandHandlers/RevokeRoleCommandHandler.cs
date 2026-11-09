using Gise.Exceptions;
using Gise.Identity.Application.Features.Commands.User.Request;
using Gise.Identity.Application.Interfaces;
using Gise.Identity.Application.Users;
using MediatR;

namespace Gise.Identity.Application.Features.Handlers.User.CommandHandlers;

public sealed class RevokeRoleCommandHandler : IRequestHandler<RevokeRoleCommand>
{
    private readonly IKeycloakUserAdmin _keycloak;

    public RevokeRoleCommandHandler(IKeycloakUserAdmin keycloak)
    {
        _keycloak = keycloak;
    }

    public Task Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
    {
        // İşlemi yapan hep admin olduğundan sistem hiçbir an adminsiz kalmaz.
        if (request.Role == AssignableRoles.Admin && request.UserId == request.RequestedBy)
            throw BusinessException.CannotRevokeOwnAdmin();

        return _keycloak.RemoveRealmRoleAsync(request.UserId, request.Role, cancellationToken);
    }
}
