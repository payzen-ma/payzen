using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Referentiel;

/// <summary>Liste des villes pour localisation précise</summary>
public class City : BaseEntity
{
    public required string CityName { get; set; }
    public int CountryId { get; set; }

    // Navigation properties
    public Country? Country { get; set; } = null!;
    public ICollection<Company.Company>? Companies { get; set; }
}
