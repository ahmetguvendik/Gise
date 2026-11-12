using System.Text.Json;
using Confluent.Kafka;

namespace Gise.Audit.StoreWorker;

// Kafka'daki ECS satırını (Elastic.CommonSchema.Serilog) AuditRecord'a çevirir. Biçimlendirici alanları iki yere koyuyor:
// bilinen ECS alanları iç içe (event.action → "event": {"action"}), diğerleri düz anahtarla "labels" (metinler) ya da
// "metadata" (sayılar, diziler) altında ("actor.id", "target.user.id", "actor.roles"). Mapper ikisine de bakar.
// Hiçbir mesaj atılmaz: bozuk JSON bile Payload'a ham metin olarak yazılır (denetim satırı kaybolmasın, tüketiciyi de tıkamasın).
public static class AuditRecordMapper
{
    public static AuditRecord Map(string topic, int partition, long offset, DateTime kafkaTimestampUtc, string? value)
    {
        var record = new AuditRecord
        {
            KafkaTopic = topic,
            KafkaPartition = partition,
            KafkaOffset = offset,
            OccurredAt = kafkaTimestampUtc,
            LoggedAt = kafkaTimestampUtc
        };

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(value ?? string.Empty);
        }
        catch (JsonException)
        {
            record.Message = "Denetim satırı okunamadı (geçersiz JSON).";
            record.Payload = JsonSerializer.Serialize(new { raw = value });
            return record;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                record.Message = "Denetim satırı okunamadı (nesne değil).";
                record.Payload = JsonSerializer.Serialize(new { raw = value });
                return record;
            }

            record.Payload = root.GetRawText();
            record.Message = Text(root, "message") ?? string.Empty;
            record.Service = Text(root, "service.name");
            record.ActorId = Text(root, "actor.id");
            record.ActorRoles = Strings(root, "actor.roles");
            record.Action = Text(root, "event.action");
            record.Outcome = Text(root, "event.outcome");
            record.ErrorCode = Text(root, "error.code");
            record.ClientIp = Text(root, "client.ip");
            record.TraceId = Text(root, "trace.id");
            record.StatusCode = Number(root, "http.response.status_code") ?? Number(root, "StatusCode");

            record.LoggedAt = Time(root, "@timestamp") ?? kafkaTimestampUtc;
            record.OccurredAt = Time(root, "event.start") ?? record.LoggedAt;
            record.Targets = Targets(root);
        }

        return record;
    }

    // target.* alanları, ör. {"target.event.id": "...", "target.seat.ids": ["..."]}.
    private static string Targets(JsonElement root)
    {
        var targets = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var container in new[] { "labels", "metadata" })
        {
            if (!root.TryGetProperty(container, out var section) || section.ValueKind != JsonValueKind.Object)
                continue;

            foreach (var property in section.EnumerateObject())
            {
                if (property.Name.StartsWith("target.", StringComparison.Ordinal))
                    targets[property.Name] = property.Value;
            }
        }

        return JsonSerializer.Serialize(targets);
    }

    // "http.response.status_code": biçimlendirici iç içe nesne ile noktalı anahtarı karıştırıyor: "event": {"action"} ama
    // "http": {"response.status_code": 422} ve kökte "trace.id". Her seviyede anahtar, kalan parçaların noktayla
    // birleşimi olabilir. Bulunamazsa labels ve metadata altındaki düz anahtara bakılır ("actor.id", "actor.roles").
    private static JsonElement? Find(JsonElement root, string name)
    {
        if (Walk(root, name.Split('.'), 0) is { } found)
            return found;

        foreach (var container in new[] { "labels", "metadata" })
        {
            if (root.TryGetProperty(container, out var section) && section.ValueKind == JsonValueKind.Object
                && section.TryGetProperty(name, out var flat))
                return flat;
        }

        return null;
    }

    private static JsonElement? Walk(JsonElement current, string[] parts, int index)
    {
        if (current.ValueKind != JsonValueKind.Object)
            return null;

        for (var end = index + 1; end <= parts.Length; end++)
        {
            if (!current.TryGetProperty(string.Join('.', parts[index..end]), out var next))
                continue;

            if (end == parts.Length)
                return next;

            if (Walk(next, parts, end) is { } deeper)
                return deeper;
        }

        return null;
    }

    private static string? Text(JsonElement root, string name)
        => Find(root, name) is { ValueKind: JsonValueKind.String } element && element.GetString() is { Length: > 0 } text ? text : null;

    private static int? Number(JsonElement root, string name)
        => Find(root, name) is { ValueKind: JsonValueKind.Number } element && element.TryGetInt32(out var number) ? number : null;

    private static string[] Strings(JsonElement root, string name)
        => Find(root, name) is { ValueKind: JsonValueKind.Array } element
            ? element.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToArray()
            : [];

    private static DateTime? Time(JsonElement root, string name)
        => Text(root, name) is { } text && DateTimeOffset.TryParse(text, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var time)
            ? time.UtcDateTime
            : null;
}
