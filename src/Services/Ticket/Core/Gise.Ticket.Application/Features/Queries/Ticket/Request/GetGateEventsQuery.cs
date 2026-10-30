using Gise.Ticket.Application.Features.Queries.Ticket.Response;
using MediatR;

namespace Gise.Ticket.Application.Features.Queries.Ticket.Request;

// Kapı sayfası: şu an okutması açık etkinlikler; görevli okutmadan önce birini seçer.
public sealed class GetGateEventsQuery : IRequest<IReadOnlyList<GetGateEventsResponse>>
{
}
