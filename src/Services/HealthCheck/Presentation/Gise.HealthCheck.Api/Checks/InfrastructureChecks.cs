using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.HealthCheck.Api.Checks;

public static class InfrastructureChecks
{
    // /health/infrastructure ve /health/error-rate bu etiketlerle ayrılır: panelde ve Slack'te "altyapı" ile "hata oranı"
    // ayrı kalemler.
    public const string InfrastructureTag = "infrastructure";
    public const string ErrorRateTag = "error-rate";

    // Altyapı bileşenleri ve hata oranı, appsettings'teki Infrastructure ve Alerting bölümlerinden. Hedefler ayarda olduğu
    // için orkestratör değişince (Kubernetes servis adları) yalnızca ayar değişir. Vital olmayan bileşen (log ve izleme
    // altyapısı) düşünce Degraded: sistem çalışır, panelde sarı görünür, Slack'e yazılmaz.
    public static IServiceCollection AddInfrastructureChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient(Probe.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10));

        var infrastructure = configuration.GetRequiredSection("Infrastructure");
        var timeout = TimeSpan.FromSeconds(infrastructure.GetValue<int>("TimeoutSeconds"));
        var elasticsearch = configuration.GetRequiredSection("Elasticsearch");

        var builder = services.AddHealthChecks();

        foreach (var target in infrastructure.GetRequiredSection("Targets").GetChildren())
        {
            var name = target["Name"] ?? throw new InvalidOperationException("Infrastructure:Targets: Name eksik.");
            var type = target["Type"] ?? throw new InvalidOperationException($"Infrastructure:Targets:{name}: Type eksik.");
            var address = target["Address"] ?? throw new InvalidOperationException($"Infrastructure:Targets:{name}: Address eksik.");
            var failureStatus = target.GetValue("Vital", true) ? HealthStatus.Unhealthy : HealthStatus.Degraded;
            var accepted = target.GetSection("AcceptedStatuses").Get<int[]>() ?? [];

            builder.Add(new HealthCheckRegistration(
                name,
                provider => type switch
                {
                    "tcp" => new TcpHealthCheck(address, timeout),
                    "http" => new HttpHealthCheck(provider.GetRequiredService<IHttpClientFactory>(), address, accepted),
                    "elasticsearch" => new ElasticsearchClusterHealthCheck(
                        provider.GetRequiredService<IHttpClientFactory>(), address, elasticsearch["Username"], elasticsearch["Password"]),
                    _ => throw new InvalidOperationException($"Infrastructure:Targets:{name}: bilinmeyen tür '{type}' (tcp, http, elasticsearch).")
                },
                failureStatus,
                tags: [InfrastructureTag],
                timeout: timeout + TimeSpan.FromSeconds(3)));
        }

        var errorRate = configuration.GetRequiredSection("Alerting:ErrorRate");
        builder.Add(new HealthCheckRegistration(
            "hata-orani",
            provider => new ErrorRateHealthCheck(
                provider.GetRequiredService<IHttpClientFactory>(),
                elasticsearch["Url"] ?? throw new InvalidOperationException("Elasticsearch:Url ayarı eksik."),
                errorRate["IndexPattern"] ?? throw new InvalidOperationException("Alerting:ErrorRate:IndexPattern ayarı eksik."),
                errorRate.GetValue<int>("WindowMinutes"),
                errorRate.GetValue<int>("Threshold"),
                elasticsearch["Username"],
                elasticsearch["Password"]),
            HealthStatus.Unhealthy,
            tags: [ErrorRateTag],
            timeout: TimeSpan.FromSeconds(10)));

        return services;
    }
}
