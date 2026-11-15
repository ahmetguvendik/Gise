using System.Net;
using System.Net.Sockets;
using Gise.Catalog.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Gise.Catalog.IntegrationTests.Caching;

// RedisCacheService tek başına: gidiş-dönüş, süre, bozuk kayıt ve ulaşılamayan ya da cevap vermeyen Redis.
public sealed class RedisCacheServiceTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder("redis:8.10.2-alpine").Build();
    private IConnectionMultiplexer _connection = null!;

    public async ValueTask InitializeAsync()
    {
        await _redis.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _redis.DisposeAsync();
    }

    private sealed class Sample
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime At { get; set; }
        public List<int> Items { get; set; } = [];
    }

    private sealed class CollectingLogger : ILogger<RedisCacheService>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning) Warnings.Add(formatter(state, exception));
        }
    }

    private static IConfiguration Config() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Redis:CircuitOpenSeconds"] = "30" }).Build();

    private RedisCacheService Service(IConnectionMultiplexer? connection = null, CollectingLogger? logger = null)
        => new(connection ?? _connection, logger ?? new CollectingLogger(), Config());

    [Fact]
    public async Task Yazilan_deger_ayni_haliyle_okunur_ve_silinince_kaybolur()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = Service();
        var sample = new Sample { Name = "Duman Konseri", Price = 1234.56m, At = DateTime.UtcNow, Items = [1, 2, 3] };

        await service.SetAsync("catalog:test:roundtrip", sample, TimeSpan.FromMinutes(1), ct);
        var read = await service.GetAsync<Sample>("catalog:test:roundtrip", ct);

        Assert.NotNull(read);
        Assert.Equal(sample.Name, read.Name);
        Assert.Equal(sample.Price, read.Price);
        Assert.Equal(sample.At, read.At);
        Assert.Equal(DateTimeKind.Utc, read.At.Kind);
        Assert.Equal(sample.Items, read.Items);

        await service.RemoveAsync("catalog:test:roundtrip", ct);
        Assert.Null(await service.GetAsync<Sample>("catalog:test:roundtrip", ct));
    }

    [Fact]
    public async Task Olmayan_anahtar_null_doner()
        => Assert.Null(await Service().GetAsync<Sample>("catalog:test:yok", TestContext.Current.CancellationToken));

    [Fact]
    public async Task Sure_dolunca_deger_kaybolur()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = Service();

        await service.SetAsync("catalog:test:ttl", new Sample { Name = "kısa ömürlü" }, TimeSpan.FromSeconds(1), ct);
        Assert.NotNull(await service.GetAsync<Sample>("catalog:test:ttl", ct));

        await Task.Delay(TimeSpan.FromMilliseconds(1500), ct);
        Assert.Null(await service.GetAsync<Sample>("catalog:test:ttl", ct));
    }

    // Bozuk kayıt Redis'in arızası değil: null döner, devre açılmaz, sonraki okuma ve yazma çalışır.
    [Fact]
    public async Task Bozuk_kayit_null_doner_ve_devreyi_acmaz()
    {
        var ct = TestContext.Current.CancellationToken;
        var logger = new CollectingLogger();
        var service = Service(logger: logger);
        await _connection.GetDatabase().StringSetAsync("catalog:test:bozuk", "{bu json degil");

        Assert.Null(await service.GetAsync<Sample>("catalog:test:bozuk", ct));

        await service.SetAsync("catalog:test:saglam", new Sample { Name = "sağlam" }, TimeSpan.FromMinutes(1), ct);
        Assert.Equal("sağlam", (await service.GetAsync<Sample>("catalog:test:saglam", ct))!.Name);
        Assert.Single(logger.Warnings);
    }

    // Ulaşılamayan Redis isteği düşürmez; devre ilk hatada açılır, sonraki çağrılar Redis'e hiç gitmez (tek uyarı).
    [Fact]
    public async Task Ulasilamayan_redis_istegi_dusurmez_ve_devreyi_acar()
    {
        var ct = TestContext.Current.CancellationToken;
        var logger = new CollectingLogger();
        await using var dead = await ConnectionMultiplexer.ConnectAsync(new ConfigurationOptions
        {
            EndPoints = { "127.0.0.1:1" }, AbortOnConnectFail = false, ConnectTimeout = 300, AsyncTimeout = 300
        });
        var service = Service(dead, logger);

        Assert.Null(await service.GetAsync<Sample>("catalog:test:1", ct));
        Assert.Null(await service.GetAsync<Sample>("catalog:test:2", ct));
        await service.SetAsync("catalog:test:3", new Sample(), TimeSpan.FromMinutes(1), ct);
        await service.RemoveAsync("catalog:test:4", ct);

        Assert.Single(logger.Warnings);
    }

    // Bağlantıyı kabul edip hiç cevap vermeyen Redis (takılma): istek 500'e düşmemeli, devre açılmalı.
    [Fact]
    public async Task Cevap_vermeyen_redis_istegi_dusurmez()
    {
        var ct = TestContext.Current.CancellationToken;
        var logger = new CollectingLogger();
        using var blackHole = new TcpListener(IPAddress.Loopback, 0);
        blackHole.Start();
        _ = blackHole.AcceptTcpClientAsync(ct).AsTask().ContinueWith(_ => { }, TaskScheduler.Default);
        await using var hung = await ConnectionMultiplexer.ConnectAsync(new ConfigurationOptions
        {
            EndPoints = { $"127.0.0.1:{((IPEndPoint)blackHole.LocalEndpoint).Port}" },
            AbortOnConnectFail = false, ConnectTimeout = 300, AsyncTimeout = 300
        });
        var service = Service(hung, logger);

        Assert.Null(await service.GetAsync<Sample>("catalog:test:1", ct));
        await service.SetAsync("catalog:test:2", new Sample(), TimeSpan.FromMinutes(1), ct);
        await service.RemoveAsync("catalog:test:3", ct);

        Assert.NotEmpty(logger.Warnings);
    }
}
