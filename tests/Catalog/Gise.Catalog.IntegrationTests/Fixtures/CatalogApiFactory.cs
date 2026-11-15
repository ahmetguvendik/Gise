using Gise.Catalog.Persistence.Contexts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Gise.Catalog.IntegrationTests.Fixtures;

// Her test sınıfı kendi PostgreSQL ve Redis konteynerini alır; şema gerçek migration'larla kurulur, önbellek gerçek Redis'tir.
public sealed class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:8.10.2-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Catalog", _postgres.GetConnectionString());
        builder.UseSetting("Redis:Connection", _redis.GetConnectionString());

        builder.ConfigureTestServices(services => services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));
    }

    public HttpClient CreateClientAs(string user, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, user);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', roles));
        return client;
    }

    // Testler önbelleğin içine bakar: anahtar yazıldı mı, komut sildi mi.
    public IDatabase Cache => Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

    // Önbellek erişilemezken API'nin çalışmaya devam ettiğini denemek için.
    public Task StopRedisAsync() => _redis.StopAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
