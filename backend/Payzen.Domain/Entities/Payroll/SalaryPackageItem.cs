using Payzen.Domain.Common;
using Payzen.Domain.Entities.Payroll.Referentiel;

namespace Payzen.Domain.Entities.Payroll;

public class SalaryPackageItem : BaseEntity
{
    public int SalaryPackageId { get; set; }
    public int? PayComponentId { get; set; }
    public int? ReferentielElementId { get; set; }
    public required string Label { get; set; }
    public decimal DefaultValue { get; set; }
    public int SortOrder { get; set; }

    public required string Type { get; set; } = "allowance"; // base_salary, allowance, bonus, benefit_in_kind, social_charge
    public bool IsTaxable { get; set; } = true;
    public bool IsSocial { get; set; } = true;
    public bool IsCIMR { get; set; } = false;
    public bool IsVariable { get; set; } = false;
    public decimal? ExemptionLimit { get; set; }

    // Navigation
    public SalaryPackage? SalaryPackage { get; set; }
    public PayComponent? PayComponent { get; set; }
    public ReferentielElement? ReferentielElement { get; set; }
}
