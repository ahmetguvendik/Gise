using Gise.Catalog.Application.Features.Queries.Venue.Response;
using Mediator;

namespace Gise.Catalog.Application.Features.Queries.Venue.Request;

public sealed class GetAllVenuesQuery : IRequest<IReadOnlyList<GetAllVenuesResponse>>;
