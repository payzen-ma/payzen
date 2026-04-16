using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>
/// Legal parameters that can change over time (SMIG, SMAG, CNSS plafond, etc.)
/// Rate parameters are stored as ratios (0.0448 = 4.48%). Multiplied directly in calculations.
/// </summary>
public class LegalParameter : BaseEntity
{
    /// <summary>Immutable machine-readable key (e.g. "CNSS_PLAFOND"). Set once, never updated.</summary>
    public required string Code { get; set; }

    public required string Label { get; set; }
    public decimal Value { get; set; }
    public required string Unit { get; set; }
    public string? Source { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    // Navigation
    public virtual ICollection<RuleFormula> RuleFormulas { get; set; } = new List<RuleFormula>();

    public bool IsActive(DateOnly? asOfDate = null)
    {
        var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return EffectiveFrom <= checkDate && (EffectiveTo == null || EffectiveTo >= checkDate);
    }
}
