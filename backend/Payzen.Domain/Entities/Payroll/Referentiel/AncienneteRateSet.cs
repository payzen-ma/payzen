using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>
/// A set of seniority bonus rates (legal default or company-specific).
///
/// Design:
///   CompanyId = null, IsLegalDefault = true  → Legal minimum (backoffice)
///   CompanyId = X,    IsLegalDefault = false → Company X's custom rates (cloned from legal)
///
/// Versioning:
///   Rates are NEVER updated in place. Changes create a NEW version with a new EffectiveFrom.
///   The old version gets EffectiveTo set. This preserves historical data for payslip recalculations.
/// </summary>
public class AncienneteRateSet : BaseEntity
{

    /// <summary>null = Legal default. non-null = Company-specific rates.</summary>
    public int? CompanyId
    {
        get; set;
    }

    /// <summary>Reference to the rate set this was cloned from (null for legal defaults).</summary>
    public int? ClonedFromId
    {
        get; set;
    }

    public required string Code
    {
        get; set;
    }
    public required string Name
    {
        get; set;
    }

    /// <summary>true = Legal minimum rates (backoffice). false = Company-specific enhanced rates.</summary>
    public bool IsLegalDefault
    {
        get; set;
    }

    public string? Source
    {
        get; set;
    }
    public DateOnly EffectiveFrom
    {
        get; set;
    }
    public DateOnly? EffectiveTo
    {
        get; set;
    }

    // Navigation
    public virtual Company.Company? Company
    {
        get; set;
    }
    public virtual AncienneteRateSet? ClonedFrom
    {
        get; set;
    }
    public virtual ICollection<AncienneteRate> Rates { get; set; } = new List<AncienneteRate>();
    public virtual ICollection<AncienneteRateSet> Clones { get; set; } = new List<AncienneteRateSet>();

    public bool IsActiveOn(DateOnly date)
        => EffectiveFrom <= date && (EffectiveTo == null || EffectiveTo >= date);
}
