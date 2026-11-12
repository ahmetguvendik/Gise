using System.Net;
using System.Net.Sockets;
using Gise.HealthCheck.Api.Checks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.HealthCheck.UnitTests;

public sealed class ProbeChecksTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Tcp_open_port_is_healthy()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var result = await new TcpHealthCheck($"127.0.0.1:{port}", TimeSpan.FromSeconds(2)).CheckHealthAsync(Contexts.For(), Ct);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Theory]
    [InlineData(HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Degraded)]
    public async Task Tcp_closed_port_fails_with_the_registered_severity(HealthStatus failureStatus)
    {
        // Dinleyen yok: yeni açılıp kapatılan bir portu kullan.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var result = await new TcpHealthCheck($"127.0.0.1:{port}", TimeSpan.FromSeconds(2)).CheckHealthAsync(Contexts.For(failureStatus), Ct);

        Assert.Equal(failureStatus, result.Status);
        Assert.Contains($"127.0.0.1:{port}", result.Description);
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("postgres:abc")]
    public void Tcp_target_must_be_host_and_port(string target)
        => Assert.Throws<InvalidOperationException>(() => new TcpHealthCheck(target, TimeSpan.FromSeconds(1)));

    [Fact]
    public async Task Http_2xx_is_healthy_and_others_fail()
    {
        var ok = new HttpHealthCheck(new FakeHttpClientFactory(new FakeHandler()), "http://minio:9000/minio/health/live", []);
        var down = new HttpHealthCheck(new FakeHttpClientFactory(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))), "http://minio:9000/minio/health/live", []);

        Assert.Equal(HealthStatus.Healthy, (await ok.CheckHealthAsync(Contexts.For(), Ct)).Status);
        var failed = await down.CheckHealthAsync(Contexts.For(), Ct);
        Assert.Equal(HealthStatus.Unhealthy, failed.Status);
        Assert.Contains("503", failed.Description);
    }

    [Fact]
    public async Task Http_accepts_the_configured_statuses_only()
    {
        // Kibana kimliksiz 401 döner ama ayaktadır.
        var kibana = new HttpHealthCheck(new FakeHttpClientFactory(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized))), "http://kibana:5601/api/status", [200, 401]);
        var wrong = new HttpHealthCheck(new FakeHttpClientFactory(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))), "http://kibana:5601/api/status", [200, 401]);

        Assert.Equal(HealthStatus.Healthy, (await kibana.CheckHealthAsync(Contexts.For(), Ct)).Status);
        Assert.Equal(HealthStatus.Degraded, (await wrong.CheckHealthAsync(Contexts.For(HealthStatus.Degraded), Ct)).Status);
    }

    [Fact]
    public async Task Http_unreachable_host_fails_without_throwing()
    {
        var check = new HttpHealthCheck(new FakeHttpClientFactory(new FakeHandler(throws: new HttpRequestException("Name or service not known"))), "http://keycloak:9000/health/ready", []);

        var result = await check.CheckHealthAsync(Contexts.For(), Ct);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("keycloak:9000", result.Description);
    }

    [Theory]
    [InlineData("""{"status":"green"}""", HealthStatus.Healthy)]
    [InlineData("""{"status":"yellow"}""", HealthStatus.Healthy)]
    [InlineData("""{"status":"red"}""", HealthStatus.Unhealthy)]
    public async Task Elasticsearch_only_red_is_unhealthy(string body, HealthStatus expected)
    {
        var handler = new FakeHandler(_ => Contexts.Json(body));
        var check = new ElasticsearchClusterHealthCheck(new FakeHttpClientFactory(handler), "http://elasticsearch:9200", "health_reader", "x");

        var result = await check.CheckHealthAsync(Contexts.For(), Ct);

        Assert.Equal(expected, result.Status);
        Assert.Equal("http://elasticsearch:9200/_cluster/health", handler.Requests.Single().Uri!.ToString());
        Assert.StartsWith("Basic ", handler.Requests.Single().Authorization);
    }

    [Fact]
    public async Task Elasticsearch_rejecting_the_account_is_unhealthy()
    {
        var check = new ElasticsearchClusterHealthCheck(new FakeHttpClientFactory(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized))), "http://elasticsearch:9200", "health_reader", "yanlis");

        var result = await check.CheckHealthAsync(Contexts.For(), Ct);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("401", result.Description);
    }
}
