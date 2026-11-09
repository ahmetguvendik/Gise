using Gise.Identity.Application.Features.Commands.User.Request;
using Gise.Identity.Application.Interfaces;
using MediatR;

namespace Gise.Identity.Application.Features.Handlers.User.CommandHandlers;

public sealed class GrantRoleCommandHandler : IRequestHandler<GrantRoleCommand>
{
    private readonly IKeycloakUserAdmin _keycloak;

    public GrantRoleCommandHandler(IKeycloakUserAdmin keycloak)
    {
        _keycloak = keycloak;
    }

    // Rolün verilebilir olduğunu GrantRoleCommandValidator denetler.
    public Task Handle(GrantRoleCommand request, CancellationToken cancellationToken)
        => _keycloak.AddRealmRoleAsync(request.UserId, request.Role, cancellationToken);
}
