using System.Text.Json.Serialization;

namespace Gise.Contracts.Realtime;

// Reservation → Redis yayını → seatmap-api → tarayıcılar (SignalR): bir etkinlikte koltukların durumu değişti. Kalıcı
// değil: Redis o an yoksa ya da tarayıcının bağlantısı koptuysa mesaj kaybolur; tarayıcı yeniden bağlanınca haritayı
// baştan yükler (bkz. tasarım 3.11).
public sealed class SeatsChanged
{
    // Redis kanalı.
    public const string Channel = "seatmap:seats-changed";

    public Guid EventId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<SeatState>))]
    public SeatState State { get; set; }

    public List<Guid> SeatIds { get; set; } = [];
}
