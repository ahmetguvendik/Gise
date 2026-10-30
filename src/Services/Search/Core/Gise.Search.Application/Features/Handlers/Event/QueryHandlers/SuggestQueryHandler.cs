using Gise.Search.Application.Features.Queries.Event.Request;
using Gise.Search.Application.Features.Queries.Event.Response;
using Gise.Search.Application.Interfaces;
using MediatR;

namespace Gise.Search.Application.Features.Handlers.Event.QueryHandlers;

public sealed class SuggestQueryHandler : IRequestHandler<SuggestQuery, SuggestResponse>
{
    // Her grupta en fazla bu kadar öneri; açılır liste kısa kalsın.
    private const int SuggestionsPerGroup = 5;

    private readonly IEventIndex _eventIndex;
    private readonly TimeProvider _timeProvider;

    public SuggestQueryHandler(IEventIndex eventIndex, TimeProvider timeProvider)
    {
        _eventIndex = eventIndex;
        _timeProvider = timeProvider;
    }

    public async Task<SuggestResponse> Handle(SuggestQuery request, CancellationToken cancellationToken)
    {
        var suggestions = await _eventIndex.SuggestAsync(
            request.Q.Trim(), _timeProvider.GetUtcNow().UtcDateTime, SuggestionsPerGroup, cancellationToken);

        return new SuggestResponse
        {
            Events = suggestions.Events.Select(e => new SuggestedEvent
            {
                Id = e.Id,
                Title = e.Title,
                StartsAt = e.StartsAt,
                VenueName = e.VenueName,
                City = e.City
            }).ToList(),
            Venues = [.. suggestions.Venues],
            Cities = [.. suggestions.Cities]
        };
    }
}
