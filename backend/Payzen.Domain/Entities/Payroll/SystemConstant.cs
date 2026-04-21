using Payzen.Domain.Common;
using System;

namespace Payzen.Domain.Entities.Payroll;

public class SystemConstant : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime EffectiveDate { get; set; }
}
