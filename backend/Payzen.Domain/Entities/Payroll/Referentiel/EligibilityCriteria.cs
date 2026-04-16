using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>Defines who is eligible for certain exemptions (All, Cadres supérieurs, PDG/DG, etc.)</summary>
public class EligibilityCriteria : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Navigation
    public virtual ICollection<RulePercentage> RulePercentages { get; set; } = new List<RulePercentage>();
    public virtual ICollection<RuleVariant> RuleVariants { get; set; } = new List<RuleVariant>();
}
