using Gise.Reservation.Domain.Common;
using Gise.Reservation.Domain.Enums;

namespace Gise.Reservation.Domain.Entities;

// Bir kullanıcının bir etkinlikte tuttuğu koltuk grubu. Kilidin kendisi Redis'te; bu kayıt kimin hangi koltukları
// ne zamana kadar tuttuğunu saklar. Süresi dolan tutma "Active" kalsa da ExpiresAt geçtiği için geçersizdir.
public class SeatHold : BaseEntity
{
    public Guid EventId { get; set; }

    // Keycloak'taki kullanıcı (sub).
    public string UserId { get; set; } = string.Empty;

    // Order gelince dolar; şimdilik tutma doğrudan müşterinin.
    public Guid? OrderId { get; set; }

    public List<Guid> SeatIds { get; set; } = [];
    public DateTime ExpiresAt { get; set; }
    public HoldStatus Status { get; set; } = HoldStatus.Active;
}
