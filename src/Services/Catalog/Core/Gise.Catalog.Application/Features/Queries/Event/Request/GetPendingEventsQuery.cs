using Gise.Catalog.Application.Features.Queries.Event.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Event.Request;

// Admin'in onay bekleyenler listesi: tüm organizatörlerin onaya gönderdiği etkinlikler.
public sealed class GetPendingEventsQuery : IRequest<IReadOnlyList<GetPendingEventsResponse>>
{
}
