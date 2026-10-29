using Amazon.S3;
using Amazon.S3.Model;
using Gise.Ticket.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Gise.Ticket.Infrastructure.Storage;

// Catalog'un afişleriyle aynı bucket; bilet PDF'leri herkese açık olmayan tickets/ altında (bkz. deploy/minio/setup.sh).
public sealed class S3TicketStorage : ITicketStorage, IDisposable
{
    private readonly string _publicEndpoint;
    private readonly string _bucket;
    private readonly int _downloadUrlTtlSeconds;
    private readonly IAmazonS3 _internalClient;
    private readonly IAmazonS3 _publicClient;

    public S3TicketStorage(IConfiguration configuration)
    {
        // Ayarlar appsettings'teki Storage bölümünden okunur; anahtarlar compose'dan ortam değişkeniyle gelir.
        var storage = configuration.GetRequiredSection("Storage");
        _publicEndpoint = storage.GetValue<string>("PublicEndpoint")!;
        _bucket = storage.GetValue<string>("Bucket")!;
        _downloadUrlTtlSeconds = storage.GetValue<int>("DownloadUrlTtlSeconds");

        var accessKey = storage.GetValue<string>("AccessKey")!;
        var secretKey = storage.GetValue<string>("SecretKey")!;
        // Endpoint konteyner ağı içindir; imza host'u kapsadığı için tarayıcıya verilen adres PublicEndpoint ile imzalanır.
        _internalClient = CreateClient(storage.GetValue<string>("Endpoint")!, accessKey, secretKey);
        _publicClient = CreateClient(_publicEndpoint, accessKey, secretKey);
    }

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

    public async Task UploadPdfAsync(string key, byte[] content, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(content);
        await _internalClient.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = "application/pdf"
        }, cancellationToken);
    }

    public TicketDownload CreateDownloadUrl(string key, string fileName)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddSeconds(_downloadUrlTtlSeconds),
        };
        // Tarayıcı açmak yerine indirir.
        request.ResponseHeaderOverrides.ContentDisposition = $"attachment; filename=\"{fileName}\"";

        return new TicketDownload(MatchEndpointScheme(_publicClient.GetPreSignedURL(request)), _downloadUrlTtlSeconds);
    }

    // SDK imzalı adresi https ile üretebilir; yerelde MinIO http'dir.
    private string MatchEndpointScheme(string url)
    {
        var wantsHttp = _publicEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);

        if (wantsHttp && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return string.Concat("http://", url.AsSpan("https://".Length));

        return url;
    }

    public void Dispose()
    {
        _internalClient.Dispose();
        _publicClient.Dispose();
    }
}
