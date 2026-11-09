using Gise.Identity.Application.Auditing;
using Gise.Identity.Application.Features.Queries.Account.Request;
using Gise.Identity.Application.Features.Queries.Account.Response;
using Gise.Identity.Application.Interfaces;
using MediatR;

namespace Gise.Identity.Application.Features.Handlers.Account.QueryHandlers;

public sealed class GetMyActivityQueryHandler : IRequestHandler<GetMyActivityQuery, IReadOnlyList<GetMyActivityResponse>>
{
    // Profilde gösterilen hareket sayısı.
    private const int Shown = 20;

    // Keycloak'tan okunan: sayfa yenilemelerinin sessiz girişleri elendikten sonra yeterince hareket kalsın.
    private const int Fetched = 200;

    private readonly IKeycloakAdminClient _keycloak;

    public GetMyActivityQueryHandler(IKeycloakAdminClient keycloak)
    {
        _keycloak = keycloak;
    }

    public async Task<IReadOnlyList<GetMyActivityResponse>> Handle(GetMyActivityQuery request, CancellationToken cancellationToken)
    {
        var events = await _keycloak.GetSecurityEventsAsync(
            SecurityEventTypes.All, request.UserId, first: 0, max: Fetched, cancellationToken);

        return LoginSessions.WithoutRepeatedLogins(events)
            .Take(Shown)
            .Select(e => new GetMyActivityResponse
            {
                Time = DateTimeOffset.FromUnixTimeMilliseconds(e.Time).UtcDateTime,
                Type = e.Type,
                Failed = SecurityEventTypes.IsFailure(e.Type),
                Error = e.Error,
                CredentialType = e.CredentialType,
                IpAddress = e.IpAddress
            })
            .ToList();
    }
}
