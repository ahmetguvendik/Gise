namespace Gise.Identity.Application.Auditing;

// Son yazılan olayın zamanı (Unix ms) ve o milisaniyede yazılan olayların kimlikleri: aynı milisaniyede sonradan gelen
// olay atlanmasın, yazılan ikinci kez yazılmasın.
public sealed record KeycloakEventPosition(long Time, IReadOnlySet<string> IdsAtTime);
