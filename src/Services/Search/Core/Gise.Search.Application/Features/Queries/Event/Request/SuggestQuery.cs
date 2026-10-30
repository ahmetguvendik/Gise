using Gise.Search.Application.Features.Queries.Event.Response;
using MediatR;

namespace Gise.Search.Application.Features.Queries.Event.Request;

// Arama kutusuna yazarken öneriler: etkinlikler, mekânlar, şehirler.
public sealed class SuggestQuery : IRequest<SuggestResponse>
{
    public string Q { get; set; } = string.Empty;
}
