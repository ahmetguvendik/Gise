using System.Text.Json;
using Gise.AuditLog.Worker;

namespace Gise.AuditLog.UnitTests;

// Kafka'daki gerçek ECS satırlarının (Elastic.CommonSchema.Serilog) kolonlara çevrilmesi. Örnekler canlı akıştan alınıp
// kısaltıldı: bilinen alanlar iç içe, diğerleri labels/metadata altında düz anahtarla.
public sealed class AuditRecordMapperTests
{
    private static readonly DateTime KafkaTime = new(2026, 11, 12, 18, 0, 0, DateTimeKind.Utc);

    private const string RoleGranted = """
        {
          "@timestamp": "2026-11-12T17:55:38.4278871+00:00",
          "message": "GrantRole tamamlandı (41 ms).",
          "trace.id": "fff2d09652a129c0b72b9a52f49d2089",
          "labels": {
            "Command": "GrantRole",
            "target.user.id": "fcd6e697-0176-416b-8c24-752d3b50c271",
            "target.role.name": "organizer",
            "actor.id": "0ae0e760-9f09-416e-92bb-ece3fb133a54"
          },
          "client": { "ip": "192.168.97.1" },
          "event": { "action": "GrantRole", "dataset": "gise.audit", "outcome": "success" },
          "service": { "name": "identity-api" },
          "metadata": { "ElapsedMs": 41, "actor.roles": [ "admin", "customer" ] }
        }
        """;

    private const string RefusedRevoke = """
        {
          "@timestamp": "2026-11-12T17:57:13.3618391+00:00",
          "message": "RevokeRole başarısız: USER_CANNOT_REVOKE_OWN_ADMIN (422, 4 ms).",
          "labels": { "actor.id": "0ae0e760-9f09-416e-92bb-ece3fb133a54", "target.role.name": "admin" },
          "event": { "action": "RevokeRole", "dataset": "gise.audit", "outcome": "failure" },
          "error": { "code": "USER_CANNOT_REVOKE_OWN_ADMIN" },
          "http": { "request.id": "0HNPUJR77TT8I:00000001", "response.status_code": 422 },
          "service": { "name": "identity-api" },
          "metadata": { "actor.roles": [ "admin" ] }
        }
        """;

    private const string KeycloakLogin = """
        {
          "@timestamp": "2026-11-12T17:55:57.908331+00:00",
          "message": "Login tamamlandı (Keycloak).",
          "labels": { "actor.id": "0ae0e760-9f09-416e-92bb-ece3fb133a54" },
          "client": { "ip": "192.168.97.1" },
          "event": { "action": "Login", "outcome": "success", "provider": "keycloak", "start": "2026-11-12T17:55:37.673+00:00" },
          "service": { "name": "identity-audit-worker" }
        }
        """;

    [Fact]
    public void Maps_a_successful_human_action()
    {
        var record = AuditRecordMapper.Map("logs.audit", 2, 17, KafkaTime, RoleGranted);

        Assert.Equal(("logs.audit", 2, 17L), (record.KafkaTopic, record.KafkaPartition, record.KafkaOffset));
        Assert.Equal("identity-api", record.Service);
        Assert.Equal("0ae0e760-9f09-416e-92bb-ece3fb133a54", record.ActorId);
        Assert.Equal(["admin", "customer"], record.ActorRoles);
        Assert.Equal("GrantRole", record.Action);
        Assert.Equal("success", record.Outcome);
        Assert.Null(record.ErrorCode);
        Assert.Equal("192.168.97.1", record.ClientIp);
        Assert.Equal("fff2d09652a129c0b72b9a52f49d2089", record.TraceId);
        Assert.Equal("GrantRole tamamlandı (41 ms).", record.Message);
        Assert.Equal(new DateTime(2026, 11, 12, 17, 55, 38, DateTimeKind.Utc).AddTicks(4278871), record.OccurredAt);
        Assert.Equal(record.LoggedAt, record.OccurredAt);
    }

    [Fact]
    public void Collects_only_target_fields()
    {
        var record = AuditRecordMapper.Map("logs.audit", 0, 0, KafkaTime, RoleGranted);

        var targets = JsonSerializer.Deserialize<Dictionary<string, string>>(record.Targets)!;
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["target.role.name"] = "organizer",
                ["target.user.id"] = "fcd6e697-0176-416b-8c24-752d3b50c271"
            },
            targets);
    }

    [Fact]
    public void Maps_a_refused_attempt_with_error_and_status()
    {
        var record = AuditRecordMapper.Map("logs.audit", 0, 0, KafkaTime, RefusedRevoke);

        Assert.Equal("failure", record.Outcome);
        Assert.Equal("USER_CANNOT_REVOKE_OWN_ADMIN", record.ErrorCode);
        Assert.Equal(422, record.StatusCode);
    }

    [Fact]
    public void Finds_fields_whether_nested_dotted_or_mixed()
    {
        var nested = AuditRecordMapper.Map("logs.audit", 0, 0, KafkaTime, """{ "http": { "response": { "status_code": 403 } }, "event": { "action": "A" } }""");
        var flat = AuditRecordMapper.Map("logs.audit", 0, 0, KafkaTime, """{ "http.response.status_code": 409, "event.action": "B", "trace.id": "t" }""");

        Assert.Equal((403, "A"), (nested.StatusCode, nested.Action));
        Assert.Equal((409, "B", "t"), (flat.StatusCode, flat.Action, flat.TraceId));
    }

    [Fact]
    public void Keycloak_event_time_is_the_occurrence_time_and_the_line_time_is_kept_separately()
    {
        var record = AuditRecordMapper.Map("logs.audit", 0, 0, KafkaTime, KeycloakLogin);

        Assert.Equal(new DateTime(2026, 11, 12, 17, 55, 37, 673, DateTimeKind.Utc), record.OccurredAt);
        Assert.Equal(new DateTime(2026, 11, 12, 17, 55, 57, DateTimeKind.Utc).AddTicks(9083310), record.LoggedAt);
        Assert.Equal("Login", record.Action);
        Assert.Empty(record.ActorRoles);
        Assert.Equal("{}", record.Targets);
    }

    [Fact]
    public void Keeps_the_whole_line_as_payload()
    {
        var record = AuditRecordMapper.Map("logs.audit", 0, 0, KafkaTime, RoleGranted);

        using var payload = JsonDocument.Parse(record.Payload);
        Assert.Equal("identity-api", payload.RootElement.GetProperty("service").GetProperty("name").GetString());
        Assert.Equal("organizer", payload.RootElement.GetProperty("labels").GetProperty("target.role.name").GetString());
    }

    [Theory]
    [InlineData("{ bozuk")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("[1, 2]")]
    public void Unreadable_lines_are_kept_not_dropped(string? value)
    {
        var record = AuditRecordMapper.Map("logs.audit", 1, 9, KafkaTime, value);

        // Satır atılmaz: Kafka konumu ve ham metin saklanır, zaman Kafka'nınkidir.
        Assert.Equal((1, 9L), (record.KafkaPartition, record.KafkaOffset));
        Assert.Equal(KafkaTime, record.OccurredAt);
        Assert.Contains("okunamadı", record.Message);
        using var payload = JsonDocument.Parse(record.Payload);
        Assert.True(payload.RootElement.TryGetProperty("raw", out _));
    }

    [Fact]
    public void Missing_fields_stay_empty()
    {
        var record = AuditRecordMapper.Map("logs.audit", 0, 0, KafkaTime, """{ "message": "x" }""");

        Assert.Null(record.ActorId);
        Assert.Null(record.Action);
        Assert.Null(record.StatusCode);
        Assert.Empty(record.ActorRoles);
        Assert.Equal(KafkaTime, record.OccurredAt);
    }
}
