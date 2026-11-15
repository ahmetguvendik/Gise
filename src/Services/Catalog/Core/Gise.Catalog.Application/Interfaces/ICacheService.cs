namespace Gise.Catalog.Application.Interfaces;

// Önbellek (Redis). Telgraf'taki ICacheService'in bu serviste kullanılan kısmı: oku, yaz, sil. Önbelleğe ulaşılamazsa
// okuma boş döner, yazma ve silme sessizce geçer (bkz. RedisCacheService); istek veritabanından sürer.
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    // Birden çok anahtarı tek Redis çağrısıyla siler.
    Task RemoveAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default);
}
