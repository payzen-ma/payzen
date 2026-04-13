using Payzen.Domain.Enums;
using Payzen.Domain.Common;


namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>
/// Dual cap rule: combines a fixed cap AND a percentage cap.
/// Used for rules like DGI ticket-restaurant (20 DH/jour ET 20% du SBI).
/// The exemption is limited by BOTH conditions.
/// </summary>
public class RuleDualCap : BaseEntity
{
    public int RuleId
    {
        get; set;
    }

    // Fixed cap portion (e.g., 20 DH/jour)
    public decimal FixedCapAmount
    {
        get; set;
    }
    public CapUnit FixedCapUnit
    {
        get; set;
    }

    /// <summary>PercentageCap stored as human % (20 = 20%). Divided by 100 in calculations.</summary>
    public decimal PercentageCap
    {
        get; set;
    }
    public BaseReference BaseReference
    {
        get; set;
    }

    // MIN = most restrictive, MAX = most favorable
    public DualCapLogic Logic { get; set; } = DualCapLogic.MIN;

    // Navigation
    public virtual ElementRule Rule { get; set; } = null!;

    /// <summary>
    /// Calculate the effective cap given a base amount (salary).
    /// Returns the appropriate cap based on the Logic (MIN or MAX).
    /// </summary>
    public decimal CalculateEffectiveCap(decimal baseAmount, int periodsInUnit = 1)
    {
        var fixedCap = FixedCapAmount * periodsInUnit;
        var percentageCap = baseAmount * (PercentageCap / 100);
        return Logic == DualCapLogic.MIN
            ? Math.Min(fixedCap, percentageCap)
            : Math.Max(fixedCap, percentageCap);
    }
}
