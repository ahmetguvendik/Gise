using Gise.Catalog.Application.Features.Queries.Event.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Event.Request;

// Organizatörün kendi etkinlikleri: taslaklar dahil. Controller OrganizerId'yi token'dan doldurur.
public sealed class GetMyEventsQuery : IRequest<IReadOnlyList<GetMyEventsResponse>>
{
    public string OrganizerId { get; set; } = string.Empty;
}
