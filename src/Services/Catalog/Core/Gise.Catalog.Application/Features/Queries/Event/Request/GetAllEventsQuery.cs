using Gise.Catalog.Application.Features.Queries.Event.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Event.Request;

// Yalnızca henüz başlamamış etkinlikler döner.
public sealed class GetAllEventsQuery : IRequest<IReadOnlyList<GetAllEventsResponse>>;
