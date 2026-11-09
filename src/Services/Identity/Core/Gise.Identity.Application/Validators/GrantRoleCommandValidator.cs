using FluentValidation;
using Gise.Identity.Application.Features.Commands.User.Request;

namespace Gise.Identity.Application.Validators;

public sealed class GrantRoleCommandValidator : AbstractValidator<GrantRoleCommand>
{
    public GrantRoleCommandValidator()
    {
        RuleFor(x => x.UserId).KeycloakUserId();
        RuleFor(x => x.Role).AssignableRole();
    }
}
