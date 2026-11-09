using Gise.Identity.Application.Features.Queries.User.Response;
using MediatR;

namespace Gise.Identity.Application.Features.Queries.User.Request;

// Admin: rol vermek için kullanıcı arama (e-posta, ad ya da soyadda geçen).
public sealed class SearchUsersQuery : IRequest<IReadOnlyList<SearchUsersResponse>>
{
    public string Search { get; set; } = string.Empty;
}
