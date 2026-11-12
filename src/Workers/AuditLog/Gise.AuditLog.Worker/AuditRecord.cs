namespace Gise.AuditLog.Worker;

// Bir denetim satırı (kim ne yaptı), bkz. tasarım 8. Sorgulanan alanlar kolon; satırın tamamı Payload'da (kanıt: sonradan
// başka alan gerekirse kaybolmaz). Kafka konumu (topic, bölüm, offset) satırın tekil kimliği: aynı mesaj ikinci kez
// gelirse (offset onaylanmadan çökülürse) yazılmaz. Tablo yalnızca eklenir; tek silme saklama süresi dolanlar.
public sealed class AuditRecord
{
    public long Id { get; set; }

    public string KafkaTopic { get; set; } = string.Empty;
    public int KafkaPartition { get; set; }
    public long KafkaOffset { get; set; }

    // İşlemin gerçekleştiği an (UTC). Keycloak olaylarında Keycloak'ın kendi zamanı (event.start); diğerlerinde satırın zamanı.
    public DateTime OccurredAt { get; set; }

    // Satırın loga yazıldığı an (UTC, @timestamp).
    public DateTime LoggedAt { get; set; }

    // Satırı yazan servis (service.name): catalog-api, identity-audit-worker...
    public string? Service { get; set; }

    // İşlemi yapan kullanıcının Keycloak kimliği (sub). Kişisel veri sayılır: e-posta ve ad hiç saklanmaz.
    public string? ActorId { get; set; }

    // Yapan kullanıcının işlem anındaki rolleri (customer dışındakiler dahil, Keycloak varsayılanları hariç).
    public string[] ActorRoles { get; set; } = [];

    // Ne yapıldı: PublishEvent, GrantRole, Login, UpdateCredential...
    public string? Action { get; set; }

    // success ya da failure.
    public string? Outcome { get; set; }

    // Başarısızsa merkezi hata kodu ya da Keycloak'ın hata kodu.
    public string? ErrorCode { get; set; }

    // İstemcinin aldığı HTTP durumu (yalnızca API'deki komutlarda).
    public int? StatusCode { get; set; }

    // İşlemin geldiği adres (X-Forwarded-For'un ilki; uydurulabilir, yetki kararında kullanılmaz).
    public string? ClientIp { get; set; }

    public string? TraceId { get; set; }

    // Kime ya da neye: target.* alanları (target.event.id, target.user.id, target.role.name...), JSON.
    public string Targets { get; set; } = "{}";

    // Okunaklı özet, ör. "UpdateEventPrice tamamlandı (12 ms). Değişenler: ..." (değişiklikler burada).
    public string Message { get; set; } = string.Empty;

    // Kafka'daki satırın tamamı (ECS JSON), olduğu gibi.
    public string Payload { get; set; } = "{}";

    // Veritabanına yazıldığı an (UTC).
    public DateTime CreatedAt { get; set; }
}
