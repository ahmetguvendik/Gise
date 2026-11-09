using FluentValidation;
using Gise.Identity.Application.Users;

namespace Gise.Identity.Application.Validators;

// Rol verme ve almanın ortak kuralları.
internal static class RoleCommandRules
{
    public static IRuleBuilderOptions<T, string> AssignableRole<T>(this IRuleBuilder<T, string> rule)
        => rule.Must(AssignableRoles.Contains)
            .WithMessage($"Rol şunlardan biri olmalı: {string.Join(", ", AssignableRoles.All)}.");

    // Keycloak kullanıcı kimliği UUID; adres yoluna başka bir şey girmesin.
    public static IRuleBuilderOptions<T, string> KeycloakUserId<T>(this IRuleBuilder<T, string> rule)
        => rule.Must(id => Guid.TryParse(id, out _)).WithMessage("Geçersiz kullanıcı kimliği.");
}
