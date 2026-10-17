using Gise.Catalog.Domain.Common;
using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.Domain.Entities;

// Mekân, bölümlerini ve onların koltuklarını kendisi yönetir; yerleşim tek seferde kaydedilir.
public sealed class Venue : BaseEntity
{
    public const int NameMaxLength = 200;
    public const int CityMaxLength = 100;

    private readonly List<Section> _sections = [];

    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public IReadOnlyCollection<Section> Sections => _sections;

    private Venue() { }

    public static Venue Create(string name, string city)
    {
        name = name.Trim();
        city = city.Trim();

        if (name.Length is 0 or > NameMaxLength)
            throw new DomainException($"Mekân adı boş olamaz ve en fazla {NameMaxLength} karakter olabilir.");

        if (city.Length is 0 or > CityMaxLength)
            throw new DomainException($"Şehir boş olamaz ve en fazla {CityMaxLength} karakter olabilir.");

        return new Venue { Name = name, City = city };
    }

    public Section AddSection(string name)
    {
        var section = Section.Create(Id, name);

        if (_sections.Any(s => string.Equals(s.Name, section.Name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"'{section.Name}' adlı bölüm zaten var.");

        _sections.Add(section);
        return section;
    }
}
