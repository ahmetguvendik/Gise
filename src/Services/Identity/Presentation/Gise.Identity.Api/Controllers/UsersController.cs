using Gise.Exceptions;
using Gise.Identity.Application.Common;
using Gise.Identity.Api.Authentication;
using Gise.Identity.Application.Features.Commands.User.Request;
using Gise.Identity.Application.Features.Queries.User.Request;
using Gise.Identity.Application.Features.Queries.User.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Identity.Api.Controllers;

// Admin: kullanıcı arama ve rol verme/alma (organizer, gate-staff, admin). Kullanıcılar Keycloak'ta; değişiklik
// kullanıcının bir sonraki token'ında (en geç 5 dk) görünür. Her işlem denetim loguna yazılır.
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // search boşsa bütün kullanıcılar, doluysa e-posta, ad ya da soyadda geçen (2-100 karakter). page 1'den başlar,
    // pageSize en fazla 50 (varsayılan 10); her kullanıcı uygulamadan yönetilen rolleriyle. totalCount toplam kullanıcı.
    [HttpGet]
    public async Task<ActionResult<PagedResult<SearchUsersResponse>>> Search([FromQuery] SearchUsersQuery query, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));

    // Zaten varsa da 204.
    [HttpPut("{id:guid}/roles/{role}")]
    public async Task<IActionResult> GrantRole(Guid id, string role, CancellationToken cancellationToken)
    {
        await _mediator.Send(new GrantRoleCommand { UserId = id.ToString(), Role = role, RequestedBy = CurrentUserId }, cancellationToken);
        return NoContent();
    }

    // Yoksa da 204. Admin kendi admin rolünü alamaz (422).
    [HttpDelete("{id:guid}/roles/{role}")]
    public async Task<IActionResult> RevokeRole(Guid id, string role, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeRoleCommand { UserId = id.ToString(), Role = role, RequestedBy = CurrentUserId }, cancellationToken);
        return NoContent();
    }

    private string CurrentUserId => User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub");
}
