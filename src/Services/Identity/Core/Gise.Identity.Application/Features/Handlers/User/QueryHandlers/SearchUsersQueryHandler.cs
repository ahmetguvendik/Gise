using Gise.Identity.Application.Common;
using Gise.Identity.Application.Features.Queries.User.Request;
using Gise.Identity.Application.Features.Queries.User.Response;
using Gise.Identity.Application.Interfaces;
using Gise.Identity.Application.Users;
using MediatR;

namespace Gise.Identity.Application.Features.Handlers.User.QueryHandlers;

public sealed class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, PagedResult<SearchUsersResponse>>
{
    private readonly IKeycloakUserAdmin _keycloak;

    public SearchUsersQueryHandler(IKeycloakUserAdmin keycloak)
    {
        _keycloak = keycloak;
    }

    public async Task<PagedResult<SearchUsersResponse>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var search = (request.Search ?? string.Empty).Trim();

        // Sayım ve sayfa birlikte istenir. Keycloak listeyi e-postaya göre sıralı verir; sayfalar arasında sıra tutarlı.
        var totalTask = _keycloak.CountUsersAsync(search, cancellationToken);
        var usersTask = _keycloak.SearchUsersAsync(search, (request.Page - 1) * request.PageSize, request.PageSize, cancellationToken);
        await Task.WhenAll(totalTask, usersTask);
        var users = usersTask.Result;

        // Keycloak listede rolleri vermez; her kullanıcı için ayrı okunur (sayfa başına en fazla PageSize istek, birlikte).
        var roles = await Task.WhenAll(users.Select(user => _keycloak.GetRealmRolesAsync(user.Id, cancellationToken)));

        return new PagedResult<SearchUsersResponse>
        {
            Items = users.Select((user, index) => new SearchUsersResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Enabled = user.Enabled,
                EmailVerified = user.EmailVerified,
                Roles = AssignableRoles.All.Where(roles[index].Contains).ToList()
            }).ToList(),
            TotalCount = totalTask.Result,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
