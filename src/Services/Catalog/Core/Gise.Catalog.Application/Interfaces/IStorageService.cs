namespace Gise.Catalog.Application.Interfaces;

// Afişler MinIO'da durur; dosya API'den geçmez, tarayıcı imzalı adrese doğrudan yükler.
public interface IStorageService
{
    long MaxUploadSizeBytes { get; }

    // Tür izin verilenlerden olmalı (bkz. CreateUploadUrlCommandValidator).
    PresignedUpload CreateUploadUrl(string contentType);

    // Geçici dosyayı doğrular (gerçek tür, boyut, içerik), boylarını üretir; kalıcı anahtarı döndürür
    // (ör. "posters/2026/10/<id>"). Anahtarın biçimi CommitUploadCommandValidator'da doğrulanır.
    Task<string> CommitAsync(string stagingKey, CancellationToken cancellationToken);

    string? ResolvePublicUrl(string? key);

    string? ResolveSrcset(string? key);
}

public record PresignedUpload(string UploadUrl, string Key, string ContentType, int ExpiresInSeconds);
