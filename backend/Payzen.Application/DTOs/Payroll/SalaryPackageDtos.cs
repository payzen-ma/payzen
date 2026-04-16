using System.ComponentModel.DataAnnotations;

namespace Payzen.Application.DTOs.Payroll;

// ════════════════════════════════════════════════════════════
// AUTO RULES / CIMR CONFIG  (partagés entre SalaryPackage et SalaryPreview)
// ════════════════════════════════════════════════════════════

/// <summary>
/// Auto-calculated rules based on Moroccan labor law (Morocco 2025).
/// Matches frontend AutoRules interface.
/// </summary>
public class AutoRulesDto
{
    /// <summary>
    /// Active le calcul automatique de la prime d'ancienneté.
    /// Taux : 5% (2-5a), 10% (5-12a), 15% (12-20a), 20% (20+a)
    /// </summary>
    public bool SeniorityBonusEnabled { get; set; } = true;

    public string RuleVersion { get; set; } = "MA_2025";
}

/// <summary>
/// CIMR (Caisse Interprofessionnelle Marocaine de Retraite) configuration.
/// Matches frontend CimrConfig interface.
/// </summary>
public class CimrConfigDto
{
    /// <summary>AL_KAMIL (3%-10%), AL_MOUNASSIB (6%-12%, plafonné CNSS), ou NONE</summary>
    public string Regime { get; set; } = "NONE";

    /// <summary>Taux salarial — Al Kamil: 3%-10%, Al Mounassib: 6%-12%</summary>
    public decimal EmployeeRate
    {
        get; set;
    }

    /// <summary>Taux patronal — généralement EmployeeRate × 1.3</summary>
    public decimal EmployerRate
    {
        get; set;
    }

    /// <summary>Taux patronal personnalisé (null = calcul standard)</summary>
    public decimal? CustomEmployerRate
    {
        get; set;
    }
}

// ════════════════════════════════════════════════════════════
// SALARY PACKAGE
// ════════════════════════════════════════════════════════════

public class SalaryPackageCreateDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category must be between 2 and 100 characters")]
    public string Category { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description
    {
        get; set;
    }

    [Range(0, double.MaxValue, ErrorMessage = "Base salary must be positive")]
    public decimal BaseSalary
    {
        get; set;
    }

    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("^(draft|published|deprecated)$", ErrorMessage = "Status must be one of: draft, published, deprecated")]
    public string Status { get; set; } = "draft";

    public int? CompanyId
    {
        get; set;
    }

    public int? BusinessSectorId
    {
        get; set;
    }

    [RegularExpression("^(OFFICIAL|COMPANY)$", ErrorMessage = "TemplateType must be OFFICIAL or COMPANY")]
    public string? TemplateType
    {
        get; set;
    }

    public string? RegulationVersion
    {
        get; set;
    }

    public AutoRulesDto? AutoRules
    {
        get; set;
    }
    public CimrConfigDto? CimrConfig
    {
        get; set;
    }

    /// <summary>Legacy — utiliser CimrConfig.EmployeeRate à la place</summary>
    [Range(0, 0.12, ErrorMessage = "CIMR rate must be between 0 and 12%")]
    public decimal? CimrRate
    {
        get; set;
    }

    public bool HasPrivateInsurance { get; set; } = false;

    public DateTime? ValidFrom
    {
        get; set;
    }
    public DateTime? ValidTo
    {
        get; set;
    }

    public List<SalaryPackageItemWriteDto> Items { get; set; } = new();
}

public class SalaryPackageUpdateDto
{
    [StringLength(200, MinimumLength = 2)]
    public string? Name
    {
        get; set;
    }

    [StringLength(100, MinimumLength = 2)]
    public string? Category
    {
        get; set;
    }

    [StringLength(1000)]
    public string? Description
    {
        get; set;
    }

    [Range(0, double.MaxValue)]
    public decimal? BaseSalary
    {
        get; set;
    }

    [RegularExpression("^(draft|published|deprecated)$")]
    public string? Status
    {
        get; set;
    }

    public int? CompanyId
    {
        get; set;
    }

    public int? BusinessSectorId
    {
        get; set;
    }

    [RegularExpression("^(OFFICIAL|COMPANY)$")]
    public string? TemplateType
    {
        get; set;
    }

    public string? RegulationVersion
    {
        get; set;
    }

    public AutoRulesDto? AutoRules
    {
        get; set;
    }
    public CimrConfigDto? CimrConfig
    {
        get; set;
    }

    [Range(0, 0.12)]
    public decimal? CimrRate
    {
        get; set;
    }

    public bool? HasPrivateInsurance
    {
        get; set;
    }

    public DateTime? ValidFrom
    {
        get; set;
    }
    public DateTime? ValidTo
    {
        get; set;
    }

    public List<SalaryPackageItemWriteDto>? Items
    {
        get; set;
    }
}

public class SalaryPackageReadDto
{
    public int Id
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description
    {
        get; set;
    }
    public decimal BaseSalary
    {
        get; set;
    }
    public string Status { get; set; } = string.Empty;
    public int? CompanyId
    {
        get; set;
    }
    public string? CompanyName
    {
        get; set;
    }
    public int BusinessSectorId
    {
        get; set;
    }
    public string? BusinessSectorName
    {
        get; set;
    }
    public string TemplateType { get; set; } = "OFFICIAL";
    public string RegulationVersion { get; set; } = "MA_2025";
    public AutoRulesDto? AutoRules
    {
        get; set;
    }
    public CimrConfigDto? CimrConfig
    {
        get; set;
    }

    // Origin tracking
    public string? OriginType
    {
        get; set;
    }
    public string? SourceTemplateNameSnapshot
    {
        get; set;
    }
    public DateTime? CopiedAt
    {
        get; set;
    }

    // Legacy
    public decimal? CimrRate
    {
        get; set;
    }
    public bool HasPrivateInsurance
    {
        get; set;
    }

    // Versioning
    public int Version
    {
        get; set;
    }
    public int? SourceTemplateId
    {
        get; set;
    }
    public string? SourceTemplateName
    {
        get; set;
    }
    public int? SourceTemplateVersion
    {
        get; set;
    }
    public DateTime? ValidFrom
    {
        get; set;
    }
    public DateTime? ValidTo
    {
        get; set;
    }
    public bool IsLocked
    {
        get; set;
    }

    /// <summary>True si CompanyId est null (template global)</summary>
    public bool IsGlobalTemplate => CompanyId == null;

    public List<SalaryPackageItemReadDto> Items { get; set; } = new();
    public DateTime CreatedAt
    {
        get; set;
    }
    public DateTime UpdatedAt
    {
        get; set;
    }
}

/// <summary>Cloner un template global vers une entreprise cliente</summary>
public class SalaryPackageCloneDto
{
    [Required(ErrorMessage = "Company ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Company ID must be valid")]
    public int CompanyId
    {
        get; set;
    }

    [StringLength(200, MinimumLength = 2)]
    public string? Name
    {
        get; set;
    }

    public DateTime? ValidFrom
    {
        get; set;
    }
}

/// <summary>Dupliquer un template (copie dans la même société)</summary>
public class SalaryPackageDuplicateDto
{
    /// <summary>Si null, default : "{OriginalName} (Copy)"</summary>
    [StringLength(200, MinimumLength = 2)]
    public string? Name
    {
        get; set;
    }
}

// ════════════════════════════════════════════════════════════
// SALARY PACKAGE ITEM
// ════════════════════════════════════════════════════════════

public class SalaryPackageItemWriteDto
{
    public int? Id
    {
        get; set;
    }

    /// <summary>Référence au catalogue global de composants (optionnel)</summary>
    public int? PayComponentId
    {
        get; set;
    }

    /// <summary>
    /// Référence à un élément du référentiel pour la paie pilotée par règles.
    /// Quand renseigné, le traitement CNSS/IR/CIMR est déterminé par ElementRules.
    /// </summary>
    public int? ReferentielElementId
    {
        get; set;
    }

    [Required(ErrorMessage = "Item label is required")]
    [StringLength(200, MinimumLength = 1)]
    public string Label { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Default value must be positive")]
    public decimal DefaultValue
    {
        get; set;
    }

    public int? SortOrder
    {
        get; set;
    }

    [Required(ErrorMessage = "Item type is required")]
    [RegularExpression("^(base_salary|allowance|bonus|benefit_in_kind|social_charge)$",
        ErrorMessage = "Type must be one of: base_salary, allowance, bonus, benefit_in_kind, social_charge")]
    public string Type { get; set; } = "allowance";

    /// <summary>Soumis à l'IR (Impôt sur le Revenu)</summary>
    public bool IsTaxable { get; set; } = true;

    /// <summary>Soumis aux cotisations CNSS</summary>
    public bool IsSocial { get; set; } = true;

    /// <summary>Soumis aux cotisations CIMR</summary>
    public bool IsCIMR { get; set; } = false;

    /// <summary>Estimation mensuelle (vs montant fixe)</summary>
    public bool IsVariable { get; set; } = false;

    /// <summary>Plafond d'exonération en MAD (ex: Transport 500/750 MAD)</summary>
    [Range(0, double.MaxValue)]
    public decimal? ExemptionLimit
    {
        get; set;
    }
}

public class SalaryPackageItemReadDto
{
    public int Id
    {
        get; set;
    }
    public int? PayComponentId
    {
        get; set;
    }
    public string? PayComponentCode
    {
        get; set;
    }
    public int? ReferentielElementId
    {
        get; set;
    }
    public string? ReferentielElementName
    {
        get; set;
    }
    public string Label { get; set; } = string.Empty;
    public decimal DefaultValue
    {
        get; set;
    }
    public int SortOrder
    {
        get; set;
    }
    public string Type { get; set; } = "allowance";
    public bool IsTaxable
    {
        get; set;
    }
    public bool IsSocial
    {
        get; set;
    }
    public bool IsCIMR
    {
        get; set;
    }
    public bool IsVariable
    {
        get; set;
    }
    public decimal? ExemptionLimit
    {
        get; set;
    }
}

// ════════════════════════════════════════════════════════════
// SALARY PACKAGE ASSIGNMENT
// ════════════════════════════════════════════════════════════

public class SalaryPackageAssignmentCreateDto
{
    [Required(ErrorMessage = "Salary package is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Salary package id must be valid")]
    public int SalaryPackageId
    {
        get; set;
    }

    [Required(ErrorMessage = "Employee is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Employee id must be valid")]
    public int EmployeeId
    {
        get; set;
    }

    [Required(ErrorMessage = "Contract is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Contract id must be valid")]
    public int ContractId
    {
        get; set;
    }

    [Required(ErrorMessage = "Effective date is required")]
    public DateTime EffectiveDate
    {
        get; set;
    }
}

public class SalaryPackageAssignmentUpdateDto
{
    [DataType(DataType.Date)]
    public DateTime? EndDate
    {
        get; set;
    }
}

public class SalaryPackageAssignmentReadDto
{
    public int Id
    {
        get; set;
    }
    public int SalaryPackageId
    {
        get; set;
    }
    public string SalaryPackageName { get; set; } = string.Empty;
    public int EmployeeId
    {
        get; set;
    }
    public string EmployeeFullName { get; set; } = string.Empty;
    public int ContractId
    {
        get; set;
    }
    public int EmployeeSalaryId
    {
        get; set;
    }
    public DateTime EffectiveDate
    {
        get; set;
    }
    public DateTime? EndDate
    {
        get; set;
    }
    public DateTime CreatedAt
    {
        get; set;
    }

    /// <summary>Snapshot de la version du package au moment de l'assignation (audit/reproductibilité)</summary>
    public int PackageVersion
    {
        get; set;
    }
}

// ════════════════════════════════════════════════════════════
// PAY COMPONENT
// ════════════════════════════════════════════════════════════

public class PayComponentWriteDto
{
    [Required(ErrorMessage = "Code is required")]
    [StringLength(50, MinimumLength = 2)]
    [RegularExpression("^[A-Z0-9_]+$", ErrorMessage = "Code must contain only uppercase letters, numbers, and underscores")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "French name is required")]
    [StringLength(200, MinimumLength = 2)]
    public string NameFr { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NameAr
    {
        get; set;
    }

    [StringLength(200)]
    public string? NameEn
    {
        get; set;
    }

    [Required(ErrorMessage = "Type is required")]
    [RegularExpression("^(base_salary|allowance|bonus|benefit_in_kind|social_charge)$",
        ErrorMessage = "Type must be one of: base_salary, allowance, bonus, benefit_in_kind, social_charge")]
    public string Type { get; set; } = "allowance";

    public bool IsTaxable { get; set; } = true;
    public bool IsSocial { get; set; } = true;
    public bool IsCIMR { get; set; } = false;

    [Range(0, double.MaxValue)]
    public decimal? ExemptionLimit
    {
        get; set;
    }

    [StringLength(100)]
    public string? ExemptionRule
    {
        get; set;
    }

    [Range(0, double.MaxValue)]
    public decimal? DefaultAmount
    {
        get; set;
    }

    public DateTime? ValidFrom
    {
        get; set;
    }
    public DateTime? ValidTo
    {
        get; set;
    }

    public bool IsRegulated { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int? SortOrder
    {
        get; set;
    }
}

public class PayComponentReadDto
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public string? NameAr
    {
        get; set;
    }
    public string? NameEn
    {
        get; set;
    }
    public string Type { get; set; } = string.Empty;
    public bool IsTaxable
    {
        get; set;
    }
    public bool IsSocial
    {
        get; set;
    }
    public bool IsCIMR
    {
        get; set;
    }
    public decimal? ExemptionLimit
    {
        get; set;
    }
    public string? ExemptionRule
    {
        get; set;
    }
    public decimal? DefaultAmount
    {
        get; set;
    }
    public int Version
    {
        get; set;
    }
    public DateTime ValidFrom
    {
        get; set;
    }
    public DateTime? ValidTo
    {
        get; set;
    }
    public bool IsRegulated
    {
        get; set;
    }
    public bool IsActive
    {
        get; set;
    }
    public int SortOrder
    {
        get; set;
    }
    public DateTime CreatedAt
    {
        get; set;
    }
    public DateTime UpdatedAt
    {
        get; set;
    }
}
