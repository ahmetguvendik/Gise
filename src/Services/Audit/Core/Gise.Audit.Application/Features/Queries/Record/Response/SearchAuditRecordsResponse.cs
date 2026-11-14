namespace Gise.Audit.Application.Features.Queries.Record.Response;

public sealed class SearchAuditRecordsResponse
{
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;

    // Kaydı yazan servis: catalog-api, identity-audit-worker...
    public string Service { get; set; } = string.Empty;

    public string? ActorId { get; set; }
    public List<string> ActorRoles { get; set; } = [];
    public string? ClientIp { get; set; }
    public string? Path { get; set; }
    public string? Message { get; set; }

    // Alanda değişenler (varsa): "Event.PosterKey: boş → '...'".
    public string? Changes { get; set; }

    // İşlemin hedefi, düzleştirilmiş: "user.id", "role.name", "order.id"...
    public Dictionary<string, string> Target { get; set; } = [];
}
