namespace Gise.Catalog.Application.Caching;

// Önbellek anahtarları. "catalog:" öneki, aynı Redis'teki koltuk kilitleriyle karışmasın ve "SCAN catalog:*" ile
// görülebilsin diye. Sorguların anahtarı ile komutların sildiği anahtar aynı yerden gelir.
public static class CacheKeys
{
    public static string Event(Guid id) => $"catalog:event:{id}";

    public static string EventSummary(Guid id) => $"catalog:event-summary:{id}";

    public static string Venue(Guid id) => $"catalog:venue:{id}";

    public const string Venues = "catalog:venues";

    // Bir etkinlik değişince silinecek anahtarlar (detay ve özet).
    public static string[] ForEvent(Guid id) => [Event(id), EventSummary(id)];

    // Bir mekân ya da mekândaki etkinlik sayısı değişince silinecek anahtarlar (detay ve liste).
    public static string[] ForVenue(Guid id) => [Venue(id), Venues];
}
