using Microsoft.EntityFrameworkCore;

namespace Gise.Audit.StoreWorker;

// Denetim veritabanı (gise_audit). Şemayı bu worker kurar: başka servis bu veritabanına dokunmaz.
public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditRecord>(entity =>
        {
            entity.ToTable("audit_records");
            entity.HasKey(r => r.Id);

            entity.Property(r => r.KafkaTopic).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Service).HasMaxLength(100);
            entity.Property(r => r.ActorId).HasMaxLength(100);
            entity.Property(r => r.Action).HasMaxLength(100);
            entity.Property(r => r.Outcome).HasMaxLength(20);
            entity.Property(r => r.ErrorCode).HasMaxLength(100);
            entity.Property(r => r.ClientIp).HasMaxLength(100);
            entity.Property(r => r.TraceId).HasMaxLength(64);
            entity.Property(r => r.Targets).HasColumnType("jsonb");
            entity.Property(r => r.Payload).HasColumnType("jsonb");

            // Aynı Kafka mesajı ikinci kez yazılmaz (bkz. AuditConsumer: ON CONFLICT DO NOTHING).
            entity.HasIndex(r => new { r.KafkaTopic, r.KafkaPartition, r.KafkaOffset }).IsUnique();

            // "Bu kullanıcı ne yaptı", "bu zaman aralığında ne oldu", "bu işlem kaç kez yapıldı", saklama temizliği.
            entity.HasIndex(r => new { r.ActorId, r.OccurredAt });
            entity.HasIndex(r => r.OccurredAt);
            entity.HasIndex(r => new { r.Action, r.OccurredAt });
        });
    }
}
