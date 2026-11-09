using Gise.Identity.Application.Features.Queries.User.Request;
using Gise.Identity.Application.Features.Queries.User.Response;
using Gise.Identity.Application.Interfaces;
using Gise.Identity.Application.Users;
using MediatR;

namespace Gise.Identity.Application.Features.Handlers.User.QueryHandlers;

public sealed class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, SearchUsersResponse>
{
    // Sayfadaki kullanıcı sayısı; her birinin rolleri ayrı istekle okunur.
    private const int PageSize = 20;

    private readonly IKeycloakUserAdmin _keycloak;

    public SearchUsersQueryHandler(IKeycloakUserAdmin keycloak)
    {
        _keycloak = keycloak;
    }

    public async Task<SearchUsersResponse> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        // Bir fazlası istenir: geldiyse sonraki sayfa var (ayrıca sayım isteği gerekmez).
        var found = await _keycloak.SearchUsersAsync(request.Search.Trim(), request.First, PageSize + 1, cancellationToken);
        var users = found.Take(PageSize).ToList();

        // Keycloak listede rolleri vermez; her kullanıcı için ayrı okunur (en fazla 20 istek, birlikte).
        var roles = await Task.WhenAll(users.Select(user => _keycloak.GetRealmRolesAsync(user.Id, cancellationToken)));

        return new SearchUsersResponse
        {
            HasMore = found.Count > PageSize,
            Users = users.Select((user, index) => new SearchUsersItem
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Enabled = user.Enabled,
                EmailVerified = user.EmailVerified,
                Roles = AssignableRoles.All.Where(roles[index].Contains).ToList()
            }).ToList()
        };
    }
}
