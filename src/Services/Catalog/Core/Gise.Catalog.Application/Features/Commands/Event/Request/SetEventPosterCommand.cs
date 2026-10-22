using Gise.Catalog.Application.Features.Commands.Event.Response;
using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Catalog.Application.Features.Commands.Event.Request;

// Afiş önce /api/media ile yüklenir; dönen anahtar buraya yazılır. Boş anahtar afişi kaldırır.
public sealed class SetEventPosterCommand : IRequest<SetEventPosterResponse>
{
    public string? PosterKey { get; set; }

    // Body'den gelmez — controller route'tan ve JWT'den set eder.
    [JsonIgnore]
    public Guid EventId { get; set; }

    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsAdmin { get; set; }
}
