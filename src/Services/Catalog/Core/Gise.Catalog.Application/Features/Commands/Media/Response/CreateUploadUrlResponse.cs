namespace Gise.Catalog.Application.Features.Commands.Media.Response;

public sealed class CreateUploadUrlResponse
{
    // Tarayıcı dosyayı bu adrese PUT eder, aynı Content-Type başlığıyla.
    public string UploadUrl { get; set; } = string.Empty;

    // Geçici anahtar; yükleme bitince commit'e gönderilir.
    public string Key { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}
