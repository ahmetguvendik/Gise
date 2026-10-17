using Gise.Catalog.Domain.Common;

namespace Gise.Catalog.Domain.Entities;

// Mekândaki fiziksel koltuk. Etkinliğe özel durumu (boş/tutuldu/satıldı) Reservation servisinde tutulur.
public class Seat : BaseEntity
{
    public Guid SectionId { get; set; }
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
}
