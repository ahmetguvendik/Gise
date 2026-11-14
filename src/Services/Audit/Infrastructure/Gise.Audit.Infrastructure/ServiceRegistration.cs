using Gise.Audit.Application.Interfaces;
using Gise.Audit.Infrastructure.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Audit.Infrastructure;

public static class ServiceRegistration
{
    public const string HttpClientName = "elasticsearch";

    // Ayarlar appsettings'teki Elasticsearch bölümünden (BaseUrl, IndexPattern, Username, RequestTimeoutSeconds); parola
    // ortamdan gelir. Hesap (audit_reader) yalnızca logs-gise.audit-* akışını okuyabilir (bkz. deploy/elasticsearch/setup.sh).
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var elasticsearch = configuration.GetRequiredSection("Elasticsearch");
        var timeout = TimeSpan.FromSeconds(elasticsearch.GetValue<int>("RequestTimeoutSeconds"));

        services.AddHttpClient(HttpClientName, client => client.Timeout = timeout);
        services.AddSingleton<IAuditRecordReader, ElasticsearchAuditRecordReader>();
        services.AddSingleton<ElasticsearchHealthCheck>();

        return services;
    }
}
