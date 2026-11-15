using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Queries.Venue.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Venue.Request;

public sealed class GetAllVenuesQuery : IRequest<IReadOnlyList<GetAllVenuesResponse>>, ICacheableQuery
{
    public string CacheKey => CacheKeys.Venues;

    public TimeSpan Duration => TimeSpan.FromMinutes(5);
}
