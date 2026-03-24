using Payzen.Domain.Enums;
using Payzen.Domain.Common;


namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>
/// Percentage-based exemption details.
/// Percentage is stored as human-readable % (35 = 35%). Divided by 100 in calculations.
/// </summary>
public class RulePercentage : BaseEntity
{
    public int RuleId { get; set; }

    /// <summary>Stored as human % (35 = 35%). Divided by 100 in calculations.</summary>
    public decimal Percentage { get; set; }

    public BaseReference BaseReference { get; set; }
    public int? EligibilityId { get; set; }

    // Navigation
    public virtual ElementRule Rule { get; set; } = null!;
    public virtual EligibilityCriteria? Eligibility { get; set; }
}
