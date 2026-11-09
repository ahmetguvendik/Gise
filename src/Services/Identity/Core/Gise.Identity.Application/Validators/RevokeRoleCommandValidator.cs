using FluentValidation;
using Gise.Identity.Application.Features.Commands.User.Request;

namespace Gise.Identity.Application.Validators;

public sealed class RevokeRoleCommandValidator : AbstractValidator<RevokeRoleCommand>
{
    public RevokeRoleCommandValidator()
    {
        RuleFor(x => x.UserId).KeycloakUserId();
        RuleFor(x => x.Role).AssignableRole();
    }
}
