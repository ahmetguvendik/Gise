using System.Net;
using Gise.HealthCheck.Api.Checks;
using Gise.HealthCheck.Api.SystemHealth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.HealthCheck.UnitTests;

public sealed class ErrorRateAndRegistrationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ErrorRateHealthCheck ErrorRate(FakeHandler handler, int threshold = 10)
        => new(new FakeHttpClientFactory(handler), "http://elasticsearch:9200", "logs-gise.api-*,logs-gise.workers-*", 5, threshold, "health_reader", "x");

    [Theory]
    [InlineData(3, HealthStatus.Healthy)]
    [InlineData(10, HealthStatus.Healthy)]
    [InlineData(11, HealthStatus.Unhealthy)]
    public async Task Error_count_above_the_threshold_is_unhealthy(int count, HealthStatus expected)
    {
        var handler = new FakeHandler(_ => Contexts.Json($$"""{"count":{{count}}}"""));

        var result = await ErrorRate(handler).CheckHealthAsync(Contexts.For(), Ct);

        Assert.Equal(expected, result.Status);
        Assert.Contains(count.ToString(), result.Description);
    }

    [Fact]
    public async Task Counts_only_error_level_in_the_window_of_api_and_worker_logs()
    {
        var handler = new FakeHandler(_ => Contexts.Json("""{"count":0}"""));

        await ErrorRate(handler).CheckHealthAsync(Contexts.For(), Ct);

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        // Denetim akışı (kişisel veri) sayılmaz ve hesabın erişimi de yok.
        Assert.Equal("http://elasticsearch:9200/logs-gise.api-*,logs-gise.workers-*/_count", request.Uri!.ToString());
        Assert.Contains("\"log.level\":\"Error\"", request.Body);
        Assert.Contains("now-5m", request.Body);
    }

    [Fact]
    public async Task A_query_that_cannot_run_is_never_reported_as_no_errors()
    {
        var rejected = await ErrorRate(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden))).CheckHealthAsync(Contexts.For(), Ct);
        var unreachable = await ErrorRate(new FakeHandler(throws: new HttpRequestException("x"))).CheckHealthAsync(Contexts.For(), Ct);

        Assert.Equal(HealthStatus.Unhealthy, rejected.Status);
        Assert.Contains("okunamadı", rejected.Description);
        Assert.Equal(HealthStatus.Unhealthy, unreachable.Status);
    }

    [Fact]
    public void Non_vital_targets_degrade_instead_of_failing_and_are_tagged()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Infrastructure:TimeoutSeconds"] = "3",
            ["Infrastructure:Targets:0:Name"] = "kafka",
            ["Infrastructure:Targets:0:Type"] = "tcp",
            ["Infrastructure:Targets:0:Address"] = "kafka:9092",
            ["Infrastructure:Targets:1:Name"] = "kibana",
            ["Infrastructure:Targets:1:Type"] = "http",
            ["Infrastructure:Targets:1:Address"] = "http://kibana:5601/api/status",
            ["Infrastructure:Targets:1:Vital"] = "false",
            ["Infrastructure:Targets:1:AcceptedStatuses:0"] = "200",
            ["Infrastructure:Targets:1:AcceptedStatuses:1"] = "401",
            ["Elasticsearch:Url"] = "http://elasticsearch:9200",
            ["Alerting:ErrorRate:IndexPattern"] = "logs-*",
            ["Alerting:ErrorRate:WindowMinutes"] = "5",
            ["Alerting:ErrorRate:Threshold"] = "10"
        }).Build();

        using var provider = new ServiceCollection().AddInfrastructureChecks(configuration).BuildServiceProvider();
        var registrations = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>().Value.Registrations.ToDictionary(r => r.Name);

        Assert.Equal(HealthStatus.Unhealthy, registrations["kafka"].FailureStatus);
        Assert.Equal(HealthStatus.Degraded, registrations["kibana"].FailureStatus);
        Assert.Contains(InfrastructureChecks.InfrastructureTag, registrations["kafka"].Tags);
        Assert.Equal(HealthStatus.Unhealthy, registrations["hata-orani"].FailureStatus);
        Assert.Contains(InfrastructureChecks.ErrorRateTag, registrations["hata-orani"].Tags);
        Assert.DoesNotContain(InfrastructureChecks.InfrastructureTag, registrations["hata-orani"].Tags);
    }

    [Fact]
    public void Unknown_target_type_is_a_configuration_error()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Infrastructure:TimeoutSeconds"] = "3",
            ["Infrastructure:Targets:0:Name"] = "x",
            ["Infrastructure:Targets:0:Type"] = "ftp",
            ["Infrastructure:Targets:0:Address"] = "x:1",
            ["Elasticsearch:Url"] = "http://elasticsearch:9200",
            ["Alerting:ErrorRate:IndexPattern"] = "logs-*",
            ["Alerting:ErrorRate:WindowMinutes"] = "5",
            ["Alerting:ErrorRate:Threshold"] = "10"
        }).Build();

        using var provider = new ServiceCollection().AddInfrastructureChecks(configuration).BuildServiceProvider();
        var registration = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>().Value.Registrations.Single(r => r.Name == "x");

        Assert.Throws<InvalidOperationException>(() => registration.Factory(provider));
    }

    [Theory]
    [InlineData("catalog-api", "API'ler")]
    [InlineData("order-saga-worker", "Worker'lar")]
    [InlineData("altyapi", "Altyapı ve izleme")]
    [InlineData("hata-orani", "Altyapı ve izleme")]
    public void Items_are_grouped_by_name(string name, string group)
        => Assert.Equal(group, SystemHealthEndpoint.GroupOf(name));

    [Theory]
    [InlineData(new[] { "Healthy", "Healthy" }, "Healthy")]
    [InlineData(new[] { "Healthy", "Degraded" }, "Degraded")]
    [InlineData(new[] { "Degraded", "Unhealthy", "Healthy" }, "Unhealthy")]
    [InlineData(new[] { "Healthy", "Unknown" }, "Unknown")]
    [InlineData(new string[0], "Unknown")]
    public void Overall_status_is_the_worst_one(string[] statuses, string expected)
        => Assert.Equal(expected, SystemHealthEndpoint.Overall(statuses));
}
