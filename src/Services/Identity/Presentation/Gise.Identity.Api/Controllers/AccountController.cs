using Gise.Exceptions;
using Gise.Identity.Application.Features.Queries.Account.Request;
using Gise.Identity.Application.Features.Queries.Account.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Identity.Api.Controllers;

// Giriş yapmış kullanıcının kendi hesabı. Ad, soyad ve oturumları tarayıcı Keycloak'ın hesap API'sinden kendi token'ıyla
// okur; burada yalnızca o token'la okunamayanlar var.
[ApiController]
[Authorize]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Son girişler (hatalı denemeler dahil) ve parola, e-posta, iki aşamalı doğrulama değişiklikleri; yeniden eskiye.
    [HttpGet("activity")]
    public async Task<ActionResult<IReadOnlyList<GetMyActivityResponse>>> GetActivity(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMyActivityQuery
        {
            UserId = User.FindFirst("sub")?.Value ?? throw UnauthorizedException.MissingClaim("sub")
        }, cancellationToken));
}
