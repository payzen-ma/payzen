using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll;

/// <summary>
/// Global pay component catalog for Moroccan payroll compliance.
/// Components define regulatory rules (tax, CNSS, CIMR, exemptions) that can be reused across packages.
/// </summary>
public class PayComponent : BaseEntity
{

    /// <summary>Unique code (e.g., "BASE_SALARY", "TRANSPORT", "PRIME_ANCIENNETE")</summary>
    public required string Code
    {
        get; set;
    }

    public required string NameFr
    {
        get; set;
    }
    public string? NameAr
    {
        get; set;
    }
    public string? NameEn
    {
        get; set;
    }

    /// <summary>base_salary, allowance, bonus, benefit_in_kind, social_charge</summary>
    public required string Type
    {
        get; set;
    }

    /// <summary>Subject to IR (Impôt sur le Revenu)</summary>
    public bool IsTaxable { get; set; } = true;

    /// <summary>Subject to CNSS contributions</summary>
    public bool IsSocial { get; set; } = true;

    /// <summary>Subject to CIMR contributions</summary>
    public bool IsCIMR { get; set; } = false;

    /// <summary>Cap for tax/social exemptions in MAD (e.g., Transport: 500/750 MAD)</summary>
    public decimal? ExemptionLimit
    {
        get; set;
    }

    public int Version { get; set; } = 1;
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo
    {
        get; set;
    }
    public bool IsRegulated { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    // Navigation
    public ICollection<SalaryPackageItem>? PackageItems
    {
        get; set;
    }
}
