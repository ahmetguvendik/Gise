namespace Gise.Catalog.Application.Caching;

// Önbelleğe alınabilen sorgu (Telgraf'taki ICacheableQuery): CachingBehavior anahtarı ve süreyi buradan okur.
public interface ICacheableQuery
{
    string CacheKey { get; }

    TimeSpan Duration { get; }
}
