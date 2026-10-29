namespace Gise.Catalog.Application.Features.Queries.Media.Response;

public sealed class GetMediaPolicyResponse
{
    public long MaxSizeBytes { get; set; }
    public List<string> AllowedContentTypes { get; set; } = [];
}
