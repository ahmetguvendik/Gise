using Gise.Catalog.Application.Behaviors;
using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gise.Catalog.UnitTests.Behaviors;

// Önbellek davranışı (Telgraf'taki CachingBehavior + CanBeCached): isabet, ıska, yazılmayan yanıt ve handler hatası.
public sealed class CachingBehaviorTests
{
    private sealed class FakeCache : ICacheService
    {
        public Dictionary<string, object> Store { get; } = [];

        public List<(string Key, TimeSpan Duration)> Writes { get; } = [];

        public int Reads { get; private set; }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            Reads++;
            return Task.FromResult(Store.TryGetValue(key, out var value) ? value as T : null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken = default) where T : class
        {
            Store[key] = value;
            Writes.Add((key, duration));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Store.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class CachedQuery : ICacheableQuery
    {
        public string CacheKey => "catalog:test:1";

        public TimeSpan Duration => TimeSpan.FromMinutes(5);
    }

    private sealed class PlainQuery;

    private sealed class Reply
    {
        public int Call { get; init; }
    }

    // İzleyiciye göre değişebilen yanıt: yalnızca CanBeCached true ise saklanır.
    private sealed class ViewerReply : ICacheableResponse
    {
        public bool CanBeCached { get; init; }
    }

    private readonly FakeCache _cache = new();

    private CachingBehavior<TRequest, TResponse> Behavior<TRequest, TResponse>() where TRequest : notnull where TResponse : class
        => new(_cache, NullLogger<CachingBehavior<TRequest, TResponse>>.Instance);

    [Fact]
    public async Task Onbellekte_yoksa_handler_calisir_ve_sonuc_istenen_sureyle_yazilir()
    {
        var calls = 0;

        var reply = await Behavior<CachedQuery, Reply>().Handle(new CachedQuery(), _ => Task.FromResult(new Reply { Call = ++calls }), CancellationToken.None);

        Assert.Equal(1, reply.Call);
        var write = Assert.Single(_cache.Writes);
        Assert.Equal("catalog:test:1", write.Key);
        Assert.Equal(TimeSpan.FromMinutes(5), write.Duration);
    }

    [Fact]
    public async Task Onbellekteyse_handler_calismaz_ve_saklanan_yanit_doner()
    {
        var stored = new Reply { Call = 99 };
        _cache.Store["catalog:test:1"] = stored;
        var calls = 0;

        var reply = await Behavior<CachedQuery, Reply>().Handle(new CachedQuery(), _ => Task.FromResult(new Reply { Call = ++calls }), CancellationToken.None);

        Assert.Same(stored, reply);
        Assert.Equal(0, calls);
        Assert.Empty(_cache.Writes);
    }

    [Fact]
    public async Task Ikinci_cagri_ilkinin_sonucunu_onbellekten_alir()
    {
        var calls = 0;
        var behavior = Behavior<CachedQuery, Reply>();

        await behavior.Handle(new CachedQuery(), _ => Task.FromResult(new Reply { Call = ++calls }), CancellationToken.None);
        var second = await behavior.Handle(new CachedQuery(), _ => Task.FromResult(new Reply { Call = ++calls }), CancellationToken.None);

        Assert.Equal(1, second.Call);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CanBeCached_false_olan_yanit_yazilmaz_ve_her_seferinde_handlerdan_gelir()
    {
        var calls = 0;
        var behavior = Behavior<CachedQuery, ViewerReply>();

        await behavior.Handle(new CachedQuery(), _ => { calls++; return Task.FromResult(new ViewerReply { CanBeCached = false }); }, CancellationToken.None);
        await behavior.Handle(new CachedQuery(), _ => { calls++; return Task.FromResult(new ViewerReply { CanBeCached = false }); }, CancellationToken.None);

        Assert.Empty(_cache.Writes);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task CanBeCached_true_olan_yanit_yazilir()
    {
        await Behavior<CachedQuery, ViewerReply>().Handle(new CachedQuery(), _ => Task.FromResult(new ViewerReply { CanBeCached = true }), CancellationToken.None);

        Assert.Single(_cache.Writes);
    }

    [Fact]
    public async Task Onbellekli_olmayan_istek_dogrudan_gecer_ve_onbellege_dokunulmaz()
    {
        var reply = await Behavior<PlainQuery, Reply>().Handle(new PlainQuery(), _ => Task.FromResult(new Reply { Call = 7 }), CancellationToken.None);

        Assert.Equal(7, reply.Call);
        Assert.Equal(0, _cache.Reads);
        Assert.Empty(_cache.Writes);
    }

    [Fact]
    public async Task Handler_hata_firlatirsa_hicbir_sey_yazilmaz()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Behavior<CachedQuery, Reply>().Handle(new CachedQuery(), _ => throw new InvalidOperationException("404 gibi"), CancellationToken.None));

        Assert.Empty(_cache.Writes);
        Assert.Empty(_cache.Store);
    }
}
