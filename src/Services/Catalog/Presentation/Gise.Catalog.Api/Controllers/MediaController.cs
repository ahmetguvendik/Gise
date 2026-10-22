using Gise.Catalog.Api.Authentication;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.Media;
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
    private readonly IStorageService _storageService;

    public MediaController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost("upload-url")]
    public ActionResult<PresignedUpload> CreateUploadUrl([FromBody] UploadUrlRequest request)
        => Ok(_storageService.CreateUploadUrl(request.ContentType, request.SizeBytes));

    [HttpPost("commit")]
    public async Task<ActionResult<CommitUploadResponse>> Commit([FromBody] CommitUploadRequest request, CancellationToken cancellationToken)
    {
        var key = await _storageService.CommitAsync(request.Key, cancellationToken);

        return Ok(new CommitUploadResponse
        {
            Key = key,
            Url = _storageService.ResolvePublicUrl(key)
        });
    }

    // Ön yüz dosyayı seçerken tür ve boyutu önceden kontrol eder.
    [AllowAnonymous]
    [HttpGet("policy")]
    public ActionResult<MediaPolicyResponse> GetPolicy() => Ok(new MediaPolicyResponse
    {
        MaxSizeBytes = _storageService.MaxUploadSizeBytes,
        AllowedContentTypes = [.. MediaPolicy.AllowedContentTypes]
    });
}

public sealed class UploadUrlRequest
{
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public sealed class CommitUploadRequest
{
    public string Key { get; set; } = string.Empty;
}

public sealed class CommitUploadResponse
{
    // Etkinliğin afiş alanına yazılacak değer; Url yalnızca önizleme için.
    public string Key { get; set; } = string.Empty;
    public string? Url { get; set; }
}

public sealed class MediaPolicyResponse
{
    public long MaxSizeBytes { get; set; }
    public List<string> AllowedContentTypes { get; set; } = [];
}
