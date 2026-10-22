namespace Gise.Catalog.Application.Interfaces;

// Afişler MinIO'da durur; dosya API'den geçmez, tarayıcı imzalı adrese doğrudan yükler.
public interface IStorageService
{
    long MaxUploadSizeBytes { get; }

    PresignedUpload CreateUploadUrl(string contentType, long sizeBytes);

    // Geçici dosyayı doğrular, boylarını üretir; kalıcı anahtarı döndürür (ör. "posters/2026/10/<id>").
    Task<string> CommitAsync(string stagingKey, CancellationToken cancellationToken);

    string? ResolvePublicUrl(string? key);

    string? ResolveSrcset(string? key);
}

public record PresignedUpload(string UploadUrl, string Key, string ContentType, int ExpiresInSeconds);
