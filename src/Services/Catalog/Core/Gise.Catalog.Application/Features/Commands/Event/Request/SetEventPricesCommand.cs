using Gise.Catalog.Application.Features.Commands.Event.Response;
using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Catalog.Application.Features.Commands.Event.Request;

// Etkinliğin fiyat listesi bütün olarak gönderilir; listede olmayan bölümün fiyatı kalkar.
public sealed class SetEventPricesCommand : IRequest<SetEventPricesResponse>
{
    public List<SetEventPriceItem> Prices { get; set; } = [];

    // Body'den gelmez — controller route'tan ve JWT'den set eder.
    [JsonIgnore]
    public Guid EventId { get; set; }

    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsAdmin { get; set; }
}

public sealed class SetEventPriceItem
{
    public Guid SectionId { get; set; }
    public decimal Price { get; set; }
}
