using Gise.Catalog.Api.Authentication;
using Gise.Catalog.Application.Features.Commands.Media.Request;
using Gise.Catalog.Application.Features.Commands.Media.Response;
using Gise.Catalog.Application.Features.Queries.Media.Request;
using Gise.Catalog.Application.Features.Queries.Media.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Catalog.Api.Controllers;

// Görsel yükleme üç adım: izin al (upload-url) → tarayıcı doğrudan MinIO'ya yükler → doğrulat (commit).
// Dosya API'den geçmez.
[ApiController]
[Authorize(Policy = Policies.Organizer)]
[Route("api/media")]
public sealed class MediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("upload-url")]
    public async Task<ActionResult<CreateUploadUrlResponse>> CreateUploadUrl([FromBody] CreateUploadUrlCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("commit")]
    public async Task<ActionResult<CommitUploadResponse>> Commit([FromBody] CommitUploadCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    [AllowAnonymous]
    [HttpGet("policy")]
    public async Task<ActionResult<GetMediaPolicyResponse>> GetPolicy(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMediaPolicyQuery(), cancellationToken));
}
