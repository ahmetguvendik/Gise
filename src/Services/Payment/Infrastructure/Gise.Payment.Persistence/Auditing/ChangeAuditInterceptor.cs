using System.Globalization;
using Gise.Payment.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gise.Payment.Persistence.Auditing;

// Denetim satırının "Değişenler" kısmı (bkz. CommandLoggingBehavior; Telgraf'taki ChangeAuditInterceptor). SavingChanges'te
// çalışır: kayıttan sonra eski değerler kaybolur. Yalnızca güncellenen kayıtlar yazılır; eklenen ve silinen kaydı satırın
// kendisi (command ve target.*) söyler.
internal sealed class ChangeAuditInterceptor : SaveChangesInterceptor
{
    private const int MaxValueLength = 120;

    // Satır sürümü ve zaman damgaları her güncellemede değişir, bir şey anlatmaz.
    private static readonly HashSet<string> IgnoredProperties = ["Version", "CreatedAt", "UpdatedAt"];

    // Kişisel ya da gizli: yalnızca değiştiği yazılır, değeri yazılmaz. Adı "Email" ile biten alanlar da öyle.
    private static readonly HashSet<string> HiddenProperties = [];

    private readonly IChangeAuditCollector _collector;

    public ChangeAuditInterceptor(IChangeAuditCollector collector)
    {
        _collector = collector;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            Collect(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            Collect(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void Collect(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified || IsInfrastructure(entry))
                continue;

            foreach (var property in entry.Properties)
                CollectProperty(entry.Metadata.ClrType.Name, property);
        }
    }

    // Outbox ve inbox satırları olayın tamamını taşır; kendi worker'ları ayrıca loglar. MassTransit'in tabloları da öyle.
    private static bool IsInfrastructure(EntityEntry entry)
    {
        var type = entry.Metadata.ClrType;
        return type.Name is "OutboxMessage" or "InboxMessage"
            || type.Namespace?.StartsWith("MassTransit", StringComparison.Ordinal) == true;
    }

    private void CollectProperty(string entity, PropertyEntry property)
    {
        var name = property.Metadata.Name;
        if (!property.IsModified || IgnoredProperties.Contains(name))
            return;

        var before = property.OriginalValue;
        var after = property.CurrentValue;

        // EF, kaydın tamamı Update ile işaretlendiğinde değeri değişmeyen alanları da "modified" sayar.
        if (Equals(before, after))
            return;

        if (HiddenProperties.Contains(name) || name.EndsWith("Email", StringComparison.Ordinal))
            _collector.Add($"{entity}.{name}: değişti");
        else
            _collector.Add($"{entity}.{name}: {Format(before)} → {Format(after)}");
    }

    private static string Format(object? value) => value switch
    {
        null => "boş",
        string s when s.Length > MaxValueLength => $"({s.Length} karakterlik metin)",
        string s => $"'{s}'",
        bool b => b ? "evet" : "hayır",
        DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture),
        DateTime d => d.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "boş"
    };
}
