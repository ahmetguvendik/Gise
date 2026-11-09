using Gise.Identity.Application.Common;
using Gise.Identity.Application.Features.Queries.User.Response;
using MediatR;

namespace Gise.Identity.Application.Features.Queries.User.Request;

// Admin: kullanıcı listesi, sayfalı (Telgraf'taki GetAllUsersQuery gibi). Search boşsa bütün kullanıcılar (e-postaya göre),
// doluysa e-posta, ad ya da soyadda geçenler. Search isteğe bağlı (Search API'deki gibi string?): boş "?search=" model
// bağlamada null olur, zorunlu sayılmasın.
public sealed class SearchUsersQuery : IRequest<PagedResult<SearchUsersResponse>>
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    // Sayfada 10 kullanıcı; yalnızca o sayfa çekilir.
    public int PageSize { get; set; } = 10;
}
