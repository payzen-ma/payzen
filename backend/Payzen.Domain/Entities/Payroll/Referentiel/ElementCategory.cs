using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>Category for compensation elements (Indemnité professionnelle, sociale, etc.)</summary>
public class ElementCategory : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;


    // Navigation
    public virtual ICollection<ReferentielElement> Elements { get; set; } = new List<ReferentielElement>();
}
