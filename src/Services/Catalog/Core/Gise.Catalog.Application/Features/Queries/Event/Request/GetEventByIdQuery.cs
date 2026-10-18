using Gise.Catalog.Application.Features.Queries.Event.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Event.Request;

public sealed class GetEventByIdQuery : IRequest<GetEventByIdResponse>
{
    public Guid Id { get; set; }

    // Uç herkese açık; giriş yapılmışsa controller token'dan doldurur. Taslağı yalnızca sahibi ve admin görür.
    public string? UserId { get; set; }
    public bool IsAdmin { get; set; }
}
