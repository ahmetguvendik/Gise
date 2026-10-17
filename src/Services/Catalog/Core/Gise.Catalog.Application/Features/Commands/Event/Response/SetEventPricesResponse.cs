namespace Gise.Catalog.Application.Features.Commands.Event.Response;

public sealed class SetEventPricesResponse
{
    public Guid EventId { get; set; }
    public List<SetEventPriceItemResponse> Prices { get; set; } = [];
}

public sealed class SetEventPriceItemResponse
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
