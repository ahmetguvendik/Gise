using Gise.Catalog.Domain.Common;
using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.Domain.Entities;

// Mekândaki fiziksel koltuk. Etkinliğe özel durumu (boş/tutuldu/satıldı) Reservation servisinde tutulur.
public sealed class Seat : BaseEntity
{
    public const int RowMaxLength = 10;

    public Guid SectionId { get; private set; }
    public string Row { get; private set; } = string.Empty;
    public int Number { get; private set; }

    private Seat() { }

    public static Seat Create(Guid sectionId, string row, int number)
    {
        row = row.Trim();

        if (row.Length is 0 or > RowMaxLength)
            throw new DomainException($"Sıra boş olamaz ve en fazla {RowMaxLength} karakter olabilir.");

        if (number <= 0)
            throw new DomainException("Koltuk numarası sıfırdan büyük olmalıdır.");

        return new Seat { SectionId = sectionId, Row = row, Number = number };
    }
}
