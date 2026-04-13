using Payzen.Domain.Enums;
using Payzen.Domain.Common;


namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>Formula-based exemption (e.g., 2 × SMIG)</summary>
public class RuleFormula : BaseEntity
{
    public int RuleId
    {
        get; set;
    }
    public decimal Multiplier
    {
        get; set;
    }
    public int ParameterId
    {
        get; set;
    }
    public CapUnit ResultUnit
    {
        get; set;
    }

    // Navigation
    public virtual ElementRule Rule { get; set; } = null!;
    public virtual LegalParameter Parameter { get; set; } = null!;

    /// <summary>Calculate the current cap value based on the formula</summary>
    public decimal CalculateCurrentCap() => Multiplier * Parameter.Value;
}
