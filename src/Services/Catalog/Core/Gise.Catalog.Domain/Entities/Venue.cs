using Gise.Catalog.Domain.Common;

namespace Gise.Catalog.Domain.Entities;

public class Venue : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public ICollection<Section> Sections { get; set; } = [];
}
