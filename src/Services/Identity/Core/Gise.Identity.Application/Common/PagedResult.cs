namespace Gise.Identity.Application.Common;

// Sayfalı liste; Telgraf'taki Shared.Models.PagedResult ile aynı şekil. Page 1'den başlar.
public sealed class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
