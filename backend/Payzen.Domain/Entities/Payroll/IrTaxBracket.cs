using Payzen.Domain.Common;
using System;

namespace Payzen.Domain.Entities.Payroll;

public class IrTaxBracket : BaseEntity
{
    public decimal MinIncome { get; set; }
    public decimal MaxIncome { get; set; }
    public decimal Rate { get; set; }
    public decimal Deduction { get; set; }
    public DateTime EffectiveDate { get; set; }
}
