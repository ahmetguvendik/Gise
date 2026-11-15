using System.Text.Json;
using Gise.Catalog.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Gise.Catalog.Infrastructure.Caching;

// Telgraf'taki RedisCacheService'in bu serviste kullanılan kısmı (oku, yaz, sil). Redis'e ulaşılamazsa devre kesici açılır ve
// Redis:CircuitOpenSeconds (varsayılan 10) boyunca önbellek atlanır: istekler Redis'i beklemeden veritabanından döner.
// Telgraf'tan iki fark: (1) RedisTimeoutException, RedisException'dan değil TimeoutException'dan türer; takılan bir Redis
// isteği 500'e düşürmesin diye zaman aşımı da devreyi açar. (2) Devre durumu statik değil örneğe ait (servis tek örnek);
// testlerdeki ulaşılamayan Redis başka testleri etkilemesin.
public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly TimeSpan _circuitOpenDuration;
    private long _skipUntilTicks;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger, IConfiguration configuration)
    {
        _redis = redis;
        _logger = logger;
        _circuitOpenDuration = TimeSpan.FromSeconds(configuration.GetValue("Redis:CircuitOpenSeconds", 10));
    }

    private bool IsCircuitOpen => Interlocked.Read(ref _skipUntilTicks) > DateTime.UtcNow.Ticks;

    private void OpenCircuit(Exception ex, string operation, string key)
    {
        Interlocked.Exchange(ref _skipUntilTicks, DateTime.UtcNow.Add(_circuitOpenDuration).Ticks);
        _logger.LogWarning(ex, "Önbellek erişilemiyor ({Operation}: {Key}); {Seconds} sn atlanacak.", operation, key, _circuitOpenDuration.TotalSeconds);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (IsCircuitOpen)
            return null;

        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(key);
            if (value.IsNullOrEmpty)
                return null;

            // RedisValue hem string'e hem ReadOnlySpan<byte>'a örtük dönüştüğü için aşırı yükleme belirsiz kalıyor; tür açık.
            return JsonSerializer.Deserialize<T>((string)value!, SerializerOptions);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or JsonException)
        {
            // Bozuk kayıt ya da erişilemeyen Redis → veritabanına düşülür.
            if (ex is JsonException) _logger.LogWarning(ex, "Önbellekteki kayıt çözümlenemedi: {Key}", key);
            else OpenCircuit(ex, "okuma", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken = default) where T : class
    {
        if (IsCircuitOpen)
            return;

        try
        {
            await _redis.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(value, SerializerOptions), duration);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or JsonException)
        {
            if (ex is JsonException) _logger.LogWarning(ex, "Önbelleğe yazılamadı: {Key}", key);
            else OpenCircuit(ex, "yazma", key);
        }
    }

    // Silme devre kesiciye bakmaz: silinemeyen anahtar bayat veri demek, o yüzden Redis dönmüşse hemen denenir. Bağlantı yokken
    // komut kuyruğa alınmadan düşer (FailFast), yani denemek pahalı değil. Başarısızlık günlüğe yazılır; eski kayıt en geç TTL'de biter.
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => RemoveAsync([key], cancellationToken);

    public async Task RemoveAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0)
            return;

        try
        {
            await _redis.GetDatabase().KeyDeleteAsync(keys.Select(key => (RedisKey)key).ToArray());
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            OpenCircuit(ex, "silme (kayıt TTL dolana kadar eski kalabilir)", string.Join(", ", keys));
        }
    }
}
