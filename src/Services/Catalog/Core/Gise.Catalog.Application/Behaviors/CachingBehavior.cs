using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Gise.Catalog.Application.Behaviors;

// Telgraf'taki CachingBehavior: ICacheableQuery olan sorguda önce önbelleğe bakar, yoksa handler'ı çalıştırıp sonucu yazar.
// Handler hata fırlatırsa (404, 422...) hiçbir şey yazılmaz. Telgraf'tan farkı: ICacheableResponse.CanBeCached false ise
// yanıt yazılmaz.
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : class
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery query)
            return await next(cancellationToken);

        var cached = await _cache.GetAsync<TResponse>(query.CacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Önbellekten döndü: {Key}", query.CacheKey);
            return cached;
        }

        var response = await next(cancellationToken);

        if (response is not ICacheableResponse { CanBeCached: false })
            await _cache.SetAsync(query.CacheKey, response, query.Duration, cancellationToken);

        return response;
    }
}
