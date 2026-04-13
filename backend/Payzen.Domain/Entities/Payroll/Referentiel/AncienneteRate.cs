using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>A single tier in the seniority bonus rate schedule</summary>
public class AncienneteRate : BaseEntity
{
    public int RateSetId
    {
        get; set;
    }
    public int MinYears
    {
        get; set;
    }
    public int? MaxYears
    {
        get; set;
    }
    public decimal Rate
    {
        get; set;
    }
    public int SortOrder
    {
        get; set;
    }

    // Navigation
    public virtual AncienneteRateSet RateSet { get; set; } = null!;

    /// <summary>Display label (e.g., "2 à moins de 5 ans")</summary>
    public string GetLabel()
    {
        if (MaxYears == null)
            return $"≥ {MinYears} ans";
        return $"{MinYears} à < {MaxYears + 1} ans";
    }
}
