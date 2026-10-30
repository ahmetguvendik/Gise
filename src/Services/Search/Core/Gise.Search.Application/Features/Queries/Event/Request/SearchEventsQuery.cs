using Gise.Search.Application.Features.Queries.Event.Response;
using MediatR;

namespace Gise.Search.Application.Features.Queries.Event.Request;

// Ana sayfadaki liste ve arama: boş arama yaklaşan etkinlikleri tarihe göre döndürür.
public sealed class SearchEventsQuery : IRequest<SearchEventsResponse>
{
    // Başlık, mekân ve şehirde; Türkçe karakter duyarsız, küçük yazım hatalarını tolere eder.
    public string? Q { get; set; }

    // Şehir filtresi, sonuçtaki şehir listesinden bir değer.
    public string? City { get; set; }

    // Tarih aralığı; From verilmezse ya da geçmişse şu an.
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    // 1'den başlar.
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
}
