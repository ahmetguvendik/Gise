using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Gise.Audit.Infrastructure.Elasticsearch;

// Elasticsearch'e giden istek: adres ve temel kimlik doğrulama appsettings'ten. Okuyucu ve sağlık kontrolü ortak kullanır.
internal sealed class ElasticsearchClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;
    private readonly AuthenticationHeaderValue? _authorization;

    public ElasticsearchClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection("Elasticsearch");
        _httpClientFactory = httpClientFactory;
        _baseUrl = section["BaseUrl"]!.TrimEnd('/');
        IndexPattern = section["IndexPattern"]!;

        if (section["Username"] is { Length: > 0 } username)
            _authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{section["Password"]}")));
    }

    public string IndexPattern { get; }

    public Task<HttpResponseMessage> PostAsync(string path, HttpContent content, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/{IndexPattern}/{path}") { Content = content, Headers = { Authorization = _authorization } };
        return _httpClientFactory.CreateClient(ServiceRegistration.HttpClientName).SendAsync(request, cancellationToken);
    }
}
