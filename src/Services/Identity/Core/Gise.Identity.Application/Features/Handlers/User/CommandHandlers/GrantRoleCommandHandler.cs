using Gise.Exceptions;
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
    public async Task Handle(GrantRoleCommand request, CancellationToken cancellationToken)
    {
        // Rol yalnızca e-postasını doğrulamış kullanıcıya: doğrulanmamış adres başkasının olabilir.
        var user = await _keycloak.GetUserAsync(request.UserId, cancellationToken);
        if (!user.EmailVerified)
            throw BusinessException.UserEmailNotVerified(request.UserId);

        await _keycloak.AddRealmRoleAsync(request.UserId, request.Role, cancellationToken);
    }
}
