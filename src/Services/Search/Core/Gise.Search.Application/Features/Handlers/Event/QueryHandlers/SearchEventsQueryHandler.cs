using Gise.Search.Application.Features.Queries.Event.Request;
using Gise.Search.Application.Features.Queries.Event.Response;
using Gise.Search.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Gise.Search.Application.Features.Handlers.Event.QueryHandlers;

public sealed class SearchEventsQueryHandler : IRequestHandler<SearchEventsQuery, SearchEventsResponse>
{
    private readonly IEventIndex _eventIndex;
    private readonly IPosterUrlResolver _posterUrls;
    private readonly TimeProvider _timeProvider;
    private readonly int _fewLeftThreshold;

    public SearchEventsQueryHandler(IEventIndex eventIndex, IPosterUrlResolver posterUrls, TimeProvider timeProvider, IConfiguration configuration)
    {
        _eventIndex = eventIndex;
        _posterUrls = posterUrls;
        _timeProvider = timeProvider;
        _fewLeftThreshold = configuration.GetRequiredSection("Availability").GetValue<int>("FewLeftThreshold");
    }

    public async Task<SearchEventsResponse> Handle(SearchEventsQuery request, CancellationToken cancellationToken)
    {
        // Başlamış etkinlik satışta değil; aralık geçmişten başlasa da şu andan itibaren aranır.
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var from = request.From is { } f && f.UtcDateTime > now ? f.UtcDateTime : now;

        var result = await _eventIndex.SearchAsync(new EventSearchCriteria(
            string.IsNullOrWhiteSpace(request.Q) ? null : request.Q.Trim(),
            string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            from,
            request.To?.UtcDateTime,
            (request.Page - 1) * request.PageSize,
            request.PageSize), cancellationToken);

        return new SearchEventsResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Total = result.Total,
            Items = result.Events.Select(e => new SearchEventItem
            {
                Id = e.Id,
                Title = e.Title,
                StartsAt = e.StartsAt,
                VenueName = e.VenueName,
                City = e.City,
                MinPrice = e.MinPrice,
                PosterUrl = _posterUrls.ResolveUrl(e.PosterKey),
                PosterSrcset = _posterUrls.ResolveSrcset(e.PosterKey),
                // Stok bilgisi yoksa (henüz kurulmadı) ikisi de gösterilmez.
                SoldOut = e.SeatCount > 0 && e.AvailableCount == 0,
                RemainingSeats = e.AvailableCount is > 0 and var left && left <= _fewLeftThreshold ? left : null
            }).ToList(),
            Cities = result.Cities.Select(c => new CityFacet { Name = c.City, Count = c.Count }).ToList()
        };
    }
}
