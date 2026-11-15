using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Queries.Venue.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Venue.Request;

public sealed class GetVenueByIdQuery : IRequest<GetVenueByIdResponse>, ICacheableQuery
{
    public Guid Id { get; set; }

    public string CacheKey => CacheKeys.Venue(Id);

    public TimeSpan Duration => TimeSpan.FromMinutes(5);
}
