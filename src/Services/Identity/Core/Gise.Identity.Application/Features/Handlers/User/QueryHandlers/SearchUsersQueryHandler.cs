using Gise.Identity.Application.Features.Queries.User.Request;
using Gise.Identity.Application.Features.Queries.User.Response;
using Gise.Identity.Application.Interfaces;
using Gise.Identity.Application.Users;
using MediatR;

namespace Gise.Identity.Application.Features.Handlers.User.QueryHandlers;

public sealed class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, IReadOnlyList<SearchUsersResponse>>
{
    // Bir aramada en fazla bu kadar kullanıcı; her birinin rolleri ayrı istekle okunur.
    private const int MaxResults = 20;

    private readonly IKeycloakUserAdmin _keycloak;

    public SearchUsersQueryHandler(IKeycloakUserAdmin keycloak)
    {
        _keycloak = keycloak;
    }

    public async Task<IReadOnlyList<SearchUsersResponse>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _keycloak.SearchUsersAsync(request.Search.Trim(), MaxResults, cancellationToken);

        // Keycloak arama sonucunda rolleri vermez; her kullanıcı için ayrı okunur (en fazla 20 istek, birlikte).
        var roles = await Task.WhenAll(users.Select(user => _keycloak.GetRealmRolesAsync(user.Id, cancellationToken)));

        return users.Select((user, index) => new SearchUsersResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Enabled = user.Enabled,
            EmailVerified = user.EmailVerified,
            Roles = AssignableRoles.All.Where(roles[index].Contains).ToList()
        }).ToList();
    }
}
