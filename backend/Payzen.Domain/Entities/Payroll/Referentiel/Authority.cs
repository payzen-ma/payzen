using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>Represents a regulatory authority (CNSS, IR, AMO, CIMR, etc.)</summary>
public class Authority : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<ElementRule> ElementRules { get; set; } = new List<ElementRule>();
}
