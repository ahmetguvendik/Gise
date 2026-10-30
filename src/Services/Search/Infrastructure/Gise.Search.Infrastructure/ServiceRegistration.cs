using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Gise.Search.Application.Interfaces;
using Gise.Search.Infrastructure.Elasticsearch;
using Gise.Search.Infrastructure.Posters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Search.Infrastructure;

public static class ServiceRegistration
{
    // Elasticsearch: search-api okur, search-sync-worker yazar. Ayarlar appsettings'teki Elasticsearch bölümünden;
    // parola compose'dan ortam değişkeniyle gelir.
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var elastic = configuration.GetRequiredSection("Elasticsearch");
        var settings = new ElasticsearchClientSettings(new Uri(elastic["Url"] ?? throw new InvalidOperationException("Elasticsearch:Url ayarı eksik.")))
            .Authentication(new BasicAuthentication(
                elastic["Username"] ?? throw new InvalidOperationException("Elasticsearch:Username ayarı eksik."),
                elastic["Password"] ?? throw new InvalidOperationException("Elasticsearch:Password ayarı eksik.")))
            .RequestTimeout(TimeSpan.FromSeconds(elastic.GetValue<int>("RequestTimeoutSeconds")));

        // İstemci thread-safe ve bağlantıları kendisi yönetir; uygulama boyunca tek.
        services.AddSingleton(new ElasticsearchClient(settings));
        services.AddSingleton<IEventIndex, ElasticEventIndex>();
        services.AddSingleton<IPosterUrlResolver, PosterUrlResolver>();

        return services;
    }
}
