using Gise.Catalog.Domain.Common;
using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.Domain.Entities;

public sealed class Venue : BaseEntity
{
    public const int NameMaxLength = 200;
    public const int CityMaxLength = 100;

    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;

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
}
