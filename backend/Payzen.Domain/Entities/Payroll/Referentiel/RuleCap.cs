using Payzen.Domain.Common;
using Payzen.Domain.Enums;

namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>Fixed cap for CAPPED, PERCENTAGE_CAPPED, FORMULA_CAPPED exemption types</summary>
public class RuleCap : BaseEntity
{
    public int RuleId
    {
        get; set;
    }
    public decimal CapAmount
    {
        get; set;
    }
    public CapUnit CapUnit
    {
        get; set;
    }
    public decimal? MinAmount
    {
        get; set;
    }

    // Navigation
    public virtual ElementRule Rule { get; set; } = null!;
}
