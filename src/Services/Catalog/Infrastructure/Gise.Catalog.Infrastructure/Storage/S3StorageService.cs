using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.Media;
using Gise.Exceptions;
using Microsoft.Extensions.Configuration;
using SkiaSharp;

namespace Gise.Catalog.Infrastructure.Storage;

public sealed class S3StorageService : IStorageService, IDisposable
{
    private const string StagingPrefix = "staging/";
    private const string PostersPrefix = "posters/";
    private const string VariantExtension = ".webp";

    private readonly string _publicEndpoint;
    private readonly string _bucket;
    private readonly int _uploadUrlTtlSeconds;
    private readonly int[] _variantWidths;
    private readonly int _defaultVariantWidth;
    private readonly int _maxMegapixels;
    private readonly IAmazonS3 _internalClient;
    private readonly IAmazonS3 _publicClient;

    public S3StorageService(IConfiguration configuration)
    {
        // Ayarlar appsettings'teki Storage bölümünden okunur; anahtarlar compose'dan ortam değişkeniyle gelir.
        var storage = configuration.GetRequiredSection("Storage");
        _publicEndpoint = storage.GetValue<string>("PublicEndpoint")!;
        _bucket = storage.GetValue<string>("Bucket")!;
        _uploadUrlTtlSeconds = storage.GetValue<int>("UploadUrlTtlSeconds");
        _variantWidths = storage.GetRequiredSection("VariantWidths").Get<int[]>()!;
        _defaultVariantWidth = storage.GetValue<int>("DefaultVariantWidth");
        _maxMegapixels = storage.GetValue<int>("MaxImageMegapixels");
        MaxUploadSizeBytes = storage.GetValue<int>("MaxUploadSizeMb") * 1024L * 1024L;

        var accessKey = storage.GetValue<string>("AccessKey")!;
        var secretKey = storage.GetValue<string>("SecretKey")!;
        // Endpoint konteyner ağı içindir; imza host'u kapsadığı için tarayıcıya verilen adres PublicEndpoint ile imzalanır.
        _internalClient = CreateClient(storage.GetValue<string>("Endpoint")!, accessKey, secretKey);
        _publicClient = CreateClient(_publicEndpoint, accessKey, secretKey);
    }

    public long MaxUploadSizeBytes { get; }

    private static AmazonS3Client CreateClient(string serviceUrl, string accessKey, string secretKey) => new(
        accessKey,
        secretKey,
        new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
            // MinIO "host/bucket" biçimini kullanır; AWS'in varsayılanı "bucket.host" MinIO'da çözümlenmez.
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1",
        });

    public PresignedUpload CreateUploadUrl(string contentType, long sizeBytes)
    {
        if (!MediaPolicy.IsAllowed(contentType))
            throw BusinessException.MediaTypeNotAllowed(MediaPolicy.AllowedContentTypes);

        if (sizeBytes <= 0 || sizeBytes > MaxUploadSizeBytes)
            throw BusinessException.MediaTooLarge(MaxUploadSizeBytes);

        var key = $"{StagingPrefix}{Guid.NewGuid():N}{MediaPolicy.ExtensionFor(contentType)}";

        // ContentType imzaya dahil: tarayıcı PUT'ta aynı başlığı göndermezse MinIO imzayı reddeder.
        var url = _publicClient.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddSeconds(_uploadUrlTtlSeconds),
            ContentType = contentType,
        });

        return new PresignedUpload(MatchEndpointScheme(url), key, contentType, _uploadUrlTtlSeconds);
    }

    public async Task<string> CommitAsync(string stagingKey, CancellationToken cancellationToken)
    {
        // Anahtar istemciden geliyor: staging dışındaki bir nesnenin taşınması engellenir.
        if (string.IsNullOrWhiteSpace(stagingKey)
            || !stagingKey.StartsWith(StagingPrefix, StringComparison.Ordinal)
            || stagingKey.Contains("..", StringComparison.Ordinal))
            throw BusinessException.MediaInvalidKey();

        GetObjectMetadataResponse head;
        try
        {
            head = await _internalClient.GetObjectMetadataAsync(_bucket, stagingKey, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw BusinessException.MediaNotFound();
        }

        // Buradan sonra beyana değil, depodaki gerçek dosyaya bakılır.
        var actualType = head.Headers.ContentType;

        if (head.ContentLength > MaxUploadSizeBytes)
        {
            await DiscardAsync(stagingKey, cancellationToken);
            throw BusinessException.MediaTooLarge(MaxUploadSizeBytes);
        }

        if (!MediaPolicy.IsAllowed(actualType))
        {
            await DiscardAsync(stagingKey, cancellationToken);
            throw BusinessException.MediaTypeNotAllowed(MediaPolicy.AllowedContentTypes);
        }

        if (!await HasMatchingSignatureAsync(stagingKey, actualType!, cancellationToken))
        {
            await DiscardAsync(stagingKey, cancellationToken);
            throw BusinessException.MediaCorruptContent();
        }

        // Temel anahtar uzantısız; dosyalar "-800.webp" gibi eklerle durur.
        var baseKey = $"{PostersPrefix}{DateTime.UtcNow:yyyy/MM}/{Path.GetFileNameWithoutExtension(stagingKey)}";

        try
        {
            await WriteVariantsAsync(stagingKey, baseKey, cancellationToken);
        }
        finally
        {
            await DiscardAsync(stagingKey, cancellationToken);
        }

        return baseKey;
    }

    // Tek adres istendiğinde orta boy verilir.
    public string? ResolvePublicUrl(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : $"{PublicBase}/{VariantKey(key, _defaultVariantWidth)}";

    public string? ResolveSrcset(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : string.Join(", ", _variantWidths.Select(width => $"{PublicBase}/{VariantKey(key, width)} {width}w"));

    private string PublicBase => $"{_publicEndpoint.TrimEnd('/')}/{_bucket}";

    private static string VariantKey(string baseKey, int width) => $"{baseKey}-{width}{VariantExtension}";

    // SDK imzalı adresi https ile üretebiliyor; yerelde MinIO http'den yayın yapıyor.
    private string MatchEndpointScheme(string url)
    {
        var wantsHttp = _publicEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);

        if (wantsHttp && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return string.Concat("http://", url.AsSpan("https://".Length));

        return url;
    }

    private async Task WriteVariantsAsync(string sourceKey, string baseKey, CancellationToken cancellationToken)
    {
        using var source = await _internalClient.GetObjectAsync(_bucket, sourceKey, cancellationToken);
        using var buffer = new MemoryStream();
        await source.ResponseStream.CopyToAsync(buffer, cancellationToken);

        buffer.Position = 0;
        // İmzası doğru ama yarım ya da bozuk dosya burada çözülemez.
        using var codec = SKCodec.Create(buffer) ?? throw BusinessException.MediaCorruptContent();

        // Küçük bir dosya çok büyük bir görüntüye açılabilir; belleği doldurmadan önce boyutlarına bakılır.
        if ((long)codec.Info.Width * codec.Info.Height > _maxMegapixels * 1_000_000L)
            throw BusinessException.MediaTooManyPixels(_maxMegapixels);

        using var original = SKBitmap.Decode(codec) ?? throw BusinessException.MediaCorruptContent();

        foreach (var width in _variantWidths)
        {
            // Her boy aynı kaynaktan türer; kaynaktan geniş boy istenirse büyütülmez.
            using var resized = original.Width > width
                ? original.Resize(
                    new SKImageInfo(width, (int)Math.Round(original.Height * (double)width / original.Width)),
                    new SKSamplingOptions(SKCubicResampler.Mitchell))
                : null;

            using var image = SKImage.FromBitmap(resized ?? original);
            using var encoded = image.Encode(SKEncodedImageFormat.Webp, 85);

            await _internalClient.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = VariantKey(baseKey, width),
                InputStream = encoded.AsStream(),
                ContentType = "image/webp"
            }, cancellationToken);
        }
    }

    private async Task<bool> HasMatchingSignatureAsync(string key, string contentType, CancellationToken cancellationToken)
    {
        using var response = await _internalClient.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            ByteRange = new ByteRange(0, 15),
        }, cancellationToken);

        var buffer = new byte[16];
        var read = await response.ResponseStream.ReadAsync(buffer, cancellationToken);

        return MediaPolicy.MatchesMagicBytes(contentType, buffer.AsSpan(0, read));
    }

    private Task DiscardAsync(string key, CancellationToken cancellationToken) =>
        _internalClient.DeleteObjectAsync(_bucket, key, cancellationToken);

    public void Dispose()
    {
        _internalClient.Dispose();
        _publicClient.Dispose();
    }
}
