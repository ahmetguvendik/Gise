namespace Gise.Catalog.Application.Features.Commands.Media.Response;

public sealed class CommitUploadResponse
{
    // Etkinliğin afiş alanına yazılacak değer (SetEventPosterCommand); Url yalnızca önizleme için.
    public string Key { get; set; } = string.Empty;
    public string? Url { get; set; }
}
