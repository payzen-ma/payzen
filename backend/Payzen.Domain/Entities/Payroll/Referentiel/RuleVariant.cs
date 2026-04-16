using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>Variant conditions (zone, grade, etc.) that override base rule values</summary>
public class RuleVariant : BaseEntity
{
    public int RuleId { get; set; }
    public required string VariantType { get; set; } // e.g., "ZONE", "GRADE"
    public required string VariantKey { get; set; } // e.g., "URBAN", "HORS_URBAN"
    public required string VariantLabel { get; set; } // e.g., "Zone Urbaine"
    public decimal? OverrideCap { get; set; }
    public decimal? OverridePercentage { get; set; }
    public int? EligibilityId { get; set; }
    public int SortOrder { get; set; }

    // Navigation
    public virtual ElementRule Rule { get; set; } = null!;
    public virtual EligibilityCriteria? Eligibility { get; set; }
}
