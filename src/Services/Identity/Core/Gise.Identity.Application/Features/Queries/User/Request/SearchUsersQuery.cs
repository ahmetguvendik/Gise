using Gise.Identity.Application.Features.Queries.User.Response;
using MediatR;

namespace Gise.Identity.Application.Features.Queries.User.Request;

// Admin: kullanıcı listesi. Search boşsa bütün kullanıcılar (Keycloak'ın sırasıyla, e-postaya göre), doluysa e-posta, ad ya
// da soyadda geçenler. Sayfa sayfa: First kaçıncı kullanıcıdan başlanacağı.
public sealed class SearchUsersQuery : IRequest<SearchUsersResponse>
{
    public string Search { get; set; } = string.Empty;
    public int First { get; set; }
}
