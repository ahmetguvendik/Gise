using Gise.Identity.Application.Features.Queries.Account.Response;
using MediatR;

namespace Gise.Identity.Application.Features.Queries.Account.Request;

// Kullanıcının son girişleri ve hesap güvenliği hareketleri (profil sayfası), yeniden eskiye.
public sealed class GetMyActivityQuery : IRequest<IReadOnlyList<GetMyActivityResponse>>
{
    public string UserId { get; set; } = string.Empty;
}
