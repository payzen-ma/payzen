using System.ComponentModel.DataAnnotations;
using Payzen.Domain.Enums;

namespace Payzen.Application.DTOs.Referentiel;

// ════════════════════════════════════════════════════════════
// CITY
// ════════════════════════════════════════════════════════════

public class CityCreateDto
{
    [Required(ErrorMessage = "Le nom de la ville est requis")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caractères")]
    public required string CityName
    {
        get; set;
    }

    [Required(ErrorMessage = "L'ID du pays est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du pays doit être valide")]
    public required int CountryId
    {
        get; set;
    }
}

public class CityUpdateDto
{
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caractères")]
    public string? CityName
    {
        get; set;
    }

    [Range(1, int.MaxValue, ErrorMessage = "L'ID du pays doit être valide")]
    public int? CountryId
    {
        get; set;
    }
}

public class CityReadDto
{
    public int Id
    {
        get; set;
    }
    public string CityName { get; set; } = string.Empty;
    public int CountryId
    {
        get; set;
    }
    public string CountryName { get; set; } = string.Empty;
    public DateTime CreatedAt
    {
        get; set;
    }
}

// ════════════════════════════════════════════════════════════
// COUNTRY
// ════════════════════════════════════════════════════════════

public class CountryCreateDto
{
    [Required(ErrorMessage = "Le nom du pays est requis")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caractères")]
    public required string CountryName
    {
        get; set;
    }

    [StringLength(500, ErrorMessage = "Le nom arabe ne peut pas dépasser 500 caractères")]
    public string? CountryNameAr
    {
        get; set;
    }

    [Required(ErrorMessage = "Le code pays est requis")]
    [StringLength(3, MinimumLength = 2, ErrorMessage = "Le code doit contenir 2 ou 3 caractères")]
    public required string CountryCode
    {
        get; set;
    }

    [Required(ErrorMessage = "Le code téléphonique est requis")]
    [StringLength(10, ErrorMessage = "Le code téléphonique ne peut pas dépasser 10 caractères")]
    public required string CountryPhoneCode
    {
        get; set;
    }
}

public class CountryUpdateDto
{
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caractères")]
    public string? CountryName
    {
        get; set;
    }

    [StringLength(500, ErrorMessage = "Le nom arabe ne peut pas dépasser 500 caractères")]
    public string? CountryNameAr
    {
        get; set;
    }

    [StringLength(3, MinimumLength = 2, ErrorMessage = "Le code doit contenir 2 ou 3 caractères")]
    public string? CountryCode
    {
        get; set;
    }

    [StringLength(10, ErrorMessage = "Le code téléphonique ne peut pas dépasser 10 caractères")]
    public string? CountryPhoneCode
    {
        get; set;
    }
}

public class CountryReadDto
{
    public int Id
    {
        get; set;
    }
    public string CountryName { get; set; } = string.Empty;
    public string? CountryNameAr
    {
        get; set;
    }
    public string CountryCode { get; set; } = string.Empty;
    public string CountryPhoneCode { get; set; } = string.Empty;
    public DateTime CreatedAt
    {
        get; set;
    }
}

// ════════════════════════════════════════════════════════════
// NATIONALITY
// ════════════════════════════════════════════════════════════

public class NationalityCreateDto
{
    [Required(ErrorMessage = "Le nom de la nationalité est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    public required string Name
    {
        get; set;
    }
}

public class NationalityUpdateDto
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    public string? Name
    {
        get; set;
    }
}

public class NationalityReadDto
{
    public int Id
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
}

// ════════════════════════════════════════════════════════════
// LEGAL CONTRACT TYPE (note: suffix "Dtos" conservé du source)
// ════════════════════════════════════════════════════════════

public class LegalContractTypeCreateDtos
{
    public int Id
    {
        get; set;
    }
    public required string Code
    {
        get; set;
    }  // CDI, CDD, STAGE, FREELANCE
    public required string Name
    {
        get; set;
    }
}

public class LegalContractTypeReadDtos
{
    public int Id
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
}

public class LegalContractTypeUpdateDtos
{
    public required string Code
    {
        get; set;
    }
    public required string Name
    {
        get; set;
    }
}

// ════════════════════════════════════════════════════════════
// STATE EMPLOYMENT PROGRAM (ANAPEC, IDMAJ, TAHFIZ, etc.)
// ════════════════════════════════════════════════════════════

public class StateEmploymentProgramCreateDto
{
    [Required(ErrorMessage = "Le code est requis")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Le code doit contenir entre 2 et 30 caractères")]
    public required string Code
    {
        get; set;
    }

    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caractères")]
    public required string Name
    {
        get; set;
    }

    public bool IsCnssEmployeeExempt { get; set; } = false;
    public bool IsCnssEmployerExempt { get; set; } = false;
    public bool IsIrExempt { get; set; } = false;

    [Range(1, 120, ErrorMessage = "La durée maximale doit être entre 1 et 120 mois")]
    public int? MaxDurationMonths
    {
        get; set;
    }

    [Range(0, double.MaxValue, ErrorMessage = "Le plafond salarial doit être positif")]
    public decimal? SalaryCeiling
    {
        get; set;
    }
}

public class StateEmploymentProgramUpdateDto
{
    [StringLength(30, MinimumLength = 2)]
    public string? Code
    {
        get; set;
    }

    [StringLength(200, MinimumLength = 2)]
    public string? Name
    {
        get; set;
    }

    public bool? IsCnssEmployeeExempt
    {
        get; set;
    }
    public bool? IsCnssEmployerExempt
    {
        get; set;
    }
    public bool? IsIrExempt
    {
        get; set;
    }

    [Range(1, 120)]
    public int? MaxDurationMonths
    {
        get; set;
    }

    [Range(0, double.MaxValue)]
    public decimal? SalaryCeiling
    {
        get; set;
    }
}

public class StateEmploymentProgramReadDto
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsCnssEmployeeExempt
    {
        get; set;
    }
    public bool IsCnssEmployerExempt
    {
        get; set;
    }
    public bool IsIrExempt
    {
        get; set;
    }
    public int? MaxDurationMonths
    {
        get; set;
    }
    public decimal? SalaryCeiling
    {
        get; set;
    }
    public DateTime CreatedAt
    {
        get; set;
    }
}

// ════════════════════════════════════════════════════════════
// OVERTIME RATE RULE (riche — multilingue + règles détaillées)
// ════════════════════════════════════════════════════════════

public class OvertimeRateRuleCreateDto
{
    [Required(ErrorMessage = "Le code est requis")]
    [StringLength(50, MinimumLength = 2)]
    public required string Code
    {
        get; set;
    }

    [Required(ErrorMessage = "Le nom en français est requis")]
    [StringLength(200, MinimumLength = 2)]
    public required string NameFr
    {
        get; set;
    }

    [Required(ErrorMessage = "Le nom en arabe est requis")]
    [StringLength(200, MinimumLength = 2)]
    public required string NameAr
    {
        get; set;
    }

    [Required(ErrorMessage = "Le nom en anglais est requis")]
    [StringLength(200, MinimumLength = 2)]
    public required string NameEn
    {
        get; set;
    }

    [StringLength(1000)]
    public string? Description
    {
        get; set;
    }

    [Required(ErrorMessage = "Le type d'overtime est requis")]
    public required OvertimeType AppliesTo
    {
        get; set;
    }

    [Required]
    public TimeRangeType TimeRangeType { get; set; } = TimeRangeType.AllDay;

    public TimeOnly? StartTime
    {
        get; set;
    }
    public TimeOnly? EndTime
    {
        get; set;
    }

    /// <summary>Bitmask des jours : 1=Lun, 2=Mar, 4=Mer, 8=Jeu, 16=Ven, 32=Sam, 64=Dim. NULL = tous.</summary>
    [Range(1, 127)]
    public int? ApplicableDaysOfWeek
    {
        get; set;
    }

    [Required]
    [Range(1.00, 10.00, ErrorMessage = "Le multiplicateur doit être entre 1.00 et 10.00")]
    public decimal Multiplier
    {
        get; set;
    }

    [Required]
    public MultiplierCumulationStrategy CumulationStrategy { get; set; } = MultiplierCumulationStrategy.TakeMaximum;

    [Required]
    [Range(1, 100, ErrorMessage = "La priorité doit être entre 1 et 100")]
    public int Priority
    {
        get; set;
    }

    [StringLength(50)]
    public string? Category
    {
        get; set;
    }

    public bool IsActive { get; set; } = true;
    public DateOnly? EffectiveFrom
    {
        get; set;
    }
    public DateOnly? EffectiveTo
    {
        get; set;
    }

    [Range(0.01, 24.00)]
    public decimal? MinimumDurationHours
    {
        get; set;
    }

    [Range(0.01, 24.00)]
    public decimal? MaximumDurationHours
    {
        get; set;
    }

    public bool RequiresSuperiorApproval
    {
        get; set;
    }

    [StringLength(200)]
    public string? LegalReference
    {
        get; set;
    }

    [StringLength(500)]
    [Url(ErrorMessage = "URL invalide")]
    public string? DocumentationUrl
    {
        get; set;
    }
}

public class OvertimeRateRuleUpdateDto
{
    [StringLength(200, MinimumLength = 2)]
    public string? NameFr
    {
        get; set;
    }

    [StringLength(200, MinimumLength = 2)]
    public string? NameAr
    {
        get; set;
    }

    [StringLength(200, MinimumLength = 2)]
    public string? NameEn
    {
        get; set;
    }

    [StringLength(1000)]
    public string? Description
    {
        get; set;
    }

    public OvertimeType? AppliesTo
    {
        get; set;
    }
    public TimeRangeType? TimeRangeType
    {
        get; set;
    }
    public TimeOnly? StartTime
    {
        get; set;
    }
    public TimeOnly? EndTime
    {
        get; set;
    }

    [Range(1, 127)]
    public int? ApplicableDaysOfWeek
    {
        get; set;
    }

    [Range(1.00, 10.00)]
    public decimal? Multiplier
    {
        get; set;
    }

    public MultiplierCumulationStrategy? CumulationStrategy
    {
        get; set;
    }

    [Range(1, 100)]
    public int? Priority
    {
        get; set;
    }

    [StringLength(50)]
    public string? Category
    {
        get; set;
    }

    public bool? IsActive
    {
        get; set;
    }
    public DateOnly? EffectiveFrom
    {
        get; set;
    }
    public DateOnly? EffectiveTo
    {
        get; set;
    }

    [Range(0.01, 24.00)]
    public decimal? MinimumDurationHours
    {
        get; set;
    }

    [Range(0.01, 24.00)]
    public decimal? MaximumDurationHours
    {
        get; set;
    }

    public bool? RequiresSuperiorApproval
    {
        get; set;
    }

    [StringLength(200)]
    public string? LegalReference
    {
        get; set;
    }

    [StringLength(500)]
    [Url]
    public string? DocumentationUrl
    {
        get; set;
    }
}

public class OvertimeRateRuleReadDto
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Description
    {
        get; set;
    }
    public OvertimeType AppliesTo
    {
        get; set;
    }
    public TimeRangeType TimeRangeType
    {
        get; set;
    }
    public TimeOnly? StartTime
    {
        get; set;
    }
    public TimeOnly? EndTime
    {
        get; set;
    }
    public int? ApplicableDaysOfWeek
    {
        get; set;
    }
    public decimal Multiplier
    {
        get; set;
    }
    public MultiplierCumulationStrategy CumulationStrategy
    {
        get; set;
    }
    public int Priority
    {
        get; set;
    }
    public string? Category
    {
        get; set;
    }
    public bool IsActive
    {
        get; set;
    }
    public DateOnly? EffectiveFrom
    {
        get; set;
    }
    public DateOnly? EffectiveTo
    {
        get; set;
    }
    public decimal? MinimumDurationHours
    {
        get; set;
    }
    public decimal? MaximumDurationHours
    {
        get; set;
    }
    public bool RequiresSuperiorApproval
    {
        get; set;
    }
    public string? LegalReference
    {
        get; set;
    }
    public string? DocumentationUrl
    {
        get; set;
    }
    public DateTime CreatedAt
    {
        get; set;
    }
}

// ════════════════════════════════════════════════════════════
// REFERENTIELS MULTILINGUES (Status, Gender, EducationLevel, MaritalStatus)
// ════════════════════════════════════════════════════════════

public class StatusReadDto
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public bool IsActive
    {
        get; set;
    }
    public bool AffectsAccess
    {
        get; set;
    }
    public bool AffectsPayroll
    {
        get; set;
    }
    public bool AffectsAttendance
    {
        get; set;
    }
}

public class GenderReadDto
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public bool IsActive
    {
        get; set;
    }
}

public class EducationLevelReadDto
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int LevelOrder
    {
        get; set;
    }
    public bool IsActive
    {
        get; set;
    }
}

public class MaritalStatusReadDto
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public bool IsActive
    {
        get; set;
    }
}

public class MaritalStatusCreateDto
{
    public string? Code
    {
        get; set;
    }
    [StringLength(200)]
    public string? NameFr
    {
        get; set;
    }
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
    /// <summary>Nom affiché (utilisé comme NameFr si NameFr non fourni).</summary>
    [StringLength(200)]
    public string? Name
    {
        get; set;
    }
}

public class MaritalStatusUpdateDto
{
    [StringLength(200)]
    public string? NameFr
    {
        get; set;
    }
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
    public bool? IsActive
    {
        get; set;
    }
}

public class GenderCreateDto
{
    [StringLength(50)]
    public string? Code
    {
        get; set;
    }
    [StringLength(200)]
    public required string NameFr
    {
        get; set;
    }
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
}

public class GenderUpdateDto
{
    [StringLength(200)]
    public string? NameFr
    {
        get; set;
    }
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
    public bool? IsActive
    {
        get; set;
    }
}

public class StatusCreateDto
{
    [StringLength(50)]
    public string? Code
    {
        get; set;
    }
    [StringLength(200)]
    public required string NameFr
    {
        get; set;
    }
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
    public bool AffectsAccess
    {
        get; set;
    }
    public bool AffectsPayroll
    {
        get; set;
    }
    public bool AffectsAttendance
    {
        get; set;
    }
}

public class StatusUpdateDto
{
    [StringLength(200)]
    public string? NameFr
    {
        get; set;
    }
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
    public bool? IsActive
    {
        get; set;
    }
    public bool? AffectsAccess
    {
        get; set;
    }
    public bool? AffectsPayroll
    {
        get; set;
    }
    public bool? AffectsAttendance
    {
        get; set;
    }
}

public class EducationLevelCreateDto
{
    [StringLength(50)]
    public string? Code
    {
        get; set;
    }
    [StringLength(200)]
    public required string NameFr
    {
        get; set;
    }
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
    public int LevelOrder
    {
        get; set;
    }
}

public class EducationLevelUpdateDto
{
    [StringLength(200)]
    public string? NameFr
    {
        get; set;
    }
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
    public int? LevelOrder
    {
        get; set;
    }
    public bool? IsActive
    {
        get; set;
    }
}

// ════════════════════════════════════════════════════════════
// PAYROLL REFERENTIEL — Éléments, Règles, Autorités
// ════════════════════════════════════════════════════════════

public class AuthorityDto
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description
    {
        get; set;
    }
    public bool IsActive
    {
        get; set;
    }
}

public class BusinessSectorDto
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsStandard
    {
        get; set;
    }
    public int SortOrder
    {
        get; set;
    }
    public bool IsActive
    {
        get; set;
    }
}

public class CreateBusinessSectorDto
{
    [Required(ErrorMessage = "Le code est requis")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom est requis")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;
}

public class UpdateBusinessSectorDto
{
    [Required(ErrorMessage = "Le code est requis")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom est requis")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;
}

public class ElementCategoryDto
{
    public int Id
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public string? Description
    {
        get; set;
    }
    public int SortOrder
    {
        get; set;
    }
    public bool IsActive
    {
        get; set;
    }
}

public class CreateElementCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description
    {
        get; set;
    }
}

public class EligibilityCriteriaDto
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description
    {
        get; set;
    }
}

// ── Legal Parameter ──────────────────────────────────────────

public class LegalParameterDto
{
    public int Id
    {
        get; set;
    }
    /// <summary>Clé immuable pour les lookups (ex: "CNSS_PLAFOND").</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Libellé affiché (depuis Label en DB). Exposé comme "name" pour le front.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Description (depuis Source en DB). Exposé comme "description" pour le front.</summary>
    public string? Description
    {
        get; set;
    }
    public string? Source
    {
        get; set;
    }
    public decimal Value
    {
        get; set;
    }
    public string Unit { get; set; } = string.Empty;
    public DateOnly EffectiveFrom
    {
        get; set;
    }
    public DateOnly? EffectiveTo
    {
        get; set;
    }
    public bool IsActive
    {
        get; set;
    }
}

public class CreateLegalParameterDto
{
    public string? Code
    {
        get; set;
    }
    public required string Name
    {
        get; set;
    }
    public string? Description
    {
        get; set;
    }
    public string? Source
    {
        get; set;
    }
    public decimal Value
    {
        get; set;
    }
    public required string Unit
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
}

// ── Referentiel Element ──────────────────────────────────────

public class ReferentielElementDto
{
    public int Id
    {
        get; set;
    }
    public string? Code
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public int CategoryId
    {
        get; set;
    }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description
    {
        get; set;
    }
    public PaymentFrequency DefaultFrequency
    {
        get; set;
    }
    public ElementStatus Status
    {
        get; set;
    }
    public bool IsActive
    {
        get; set;
    }
    public bool HasConvergence
    {
        get; set;
    }
    public List<ElementRuleDto> Rules { get; set; } = new();
}

public class ReferentielElementListDto
{
    public int Id
    {
        get; set;
    }
    public string? Code
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public PaymentFrequency DefaultFrequency
    {
        get; set;
    }
    public ElementStatus Status
    {
        get; set;
    }
    public bool IsActive
    {
        get; set;
    }
    public bool HasConvergence
    {
        get; set;
    }
    public int RuleCount
    {
        get; set;
    }
    public bool HasCnssRule
    {
        get; set;
    }
    public bool HasDgiRule
    {
        get; set;
    }
}

public class CreateReferentielElementDto
{
    public string? Code
    {
        get; set;
    }
    public required string Name
    {
        get; set;
    }
    public int CategoryId
    {
        get; set;
    }
    public string? Description
    {
        get; set;
    }
    public PaymentFrequency DefaultFrequency
    {
        get; set;
    }
    public ElementStatus Status { get; set; } = ElementStatus.DRAFT;
}

public class UpdateReferentielElementDto
{
    public string? Code
    {
        get; set;
    }
    public required string Name
    {
        get; set;
    }
    public int CategoryId
    {
        get; set;
    }
    public string? Description
    {
        get; set;
    }
    public PaymentFrequency DefaultFrequency
    {
        get; set;
    }
    public bool IsActive
    {
        get; set;
    }
}

public class UpdateElementStatusDto
{
    public ElementStatus Status
    {
        get; set;
    }
}

// ── Element Rule ─────────────────────────────────────────────

public class ElementRuleDto
{
    public int Id
    {
        get; set;
    }
    public int ElementId
    {
        get; set;
    }
    public int AuthorityId
    {
        get; set;
    }
    public string AuthorityName { get; set; } = string.Empty;
    public ExemptionType ExemptionType
    {
        get; set;
    }
    public string? SourceRef
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
    public bool IsActive
    {
        get; set;
    }
    public ElementStatus Status
    {
        get; set;
    }
    public string RuleDetails { get; set; } = "{}";

    public RuleCapDto? Cap
    {
        get; set;
    }
    public RulePercentageDto? Percentage
    {
        get; set;
    }
    public RuleFormulaDto? Formula
    {
        get; set;
    }
    public RuleDualCapDto? DualCap
    {
        get; set;
    }
    public List<RuleTierDto> Tiers { get; set; } = new();
    public List<RuleVariantDto> Variants { get; set; } = new();
}

public class CreateElementRuleDto
{
    public int ElementId
    {
        get; set;
    }
    public int AuthorityId
    {
        get; set;
    }
    public string? AuthorityCode
    {
        get; set;
    }
    public ExemptionType ExemptionType
    {
        get; set;
    }
    public string? SourceRef
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
    public ElementStatus Status { get; set; } = ElementStatus.DRAFT;
    public string RuleDetails { get; set; } = "{}";
    public CreateRuleCapDto? Cap
    {
        get; set;
    }
    public CreateRulePercentageDto? Percentage
    {
        get; set;
    }
    public CreateRuleFormulaDto? Formula
    {
        get; set;
    }
    public CreateRuleDualCapDto? DualCap
    {
        get; set;
    }
    public List<CreateRuleTierDto> Tiers { get; set; } = new();
    public List<CreateRuleVariantDto> Variants { get; set; } = new();
}

public class UpdateElementRuleDto
{
    public ExemptionType? ExemptionType
    {
        get; set;
    }
    public string? SourceRef
    {
        get; set;
    }
    public DateOnly? EffectiveFrom
    {
        get; set;
    }
    public DateOnly? EffectiveTo
    {
        get; set;
    }
    public ElementStatus? Status
    {
        get; set;
    }
    public string? RuleDetails
    {
        get; set;
    }
    public CreateRuleCapDto? Cap
    {
        get; set;
    }
    public CreateRulePercentageDto? Percentage
    {
        get; set;
    }
    public CreateRuleFormulaDto? Formula
    {
        get; set;
    }
    public CreateRuleDualCapDto? DualCap
    {
        get; set;
    }
    public List<CreateRuleTierDto>? Tiers
    {
        get; set;
    }
    public List<CreateRuleVariantDto>? Variants
    {
        get; set;
    }
}

public class ValidateRuleDto
{
    public int? ElementId
    {
        get; set;
    }
    public int? AuthorityId
    {
        get; set;
    }
    public ExemptionType ExemptionType
    {
        get; set;
    }
    public string RuleDetails { get; set; } = "{}";
    public DateOnly EffectiveFrom
    {
        get; set;
    }
    public DateOnly? EffectiveTo
    {
        get; set;
    }
}

// ── Rule Details ─────────────────────────────────────────────

public class RuleCapDto
{
    public int Id
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
}
public class CreateRuleCapDto
{
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
}

public class RulePercentageDto
{
    public int Id
    {
        get; set;
    }
    public decimal Percentage
    {
        get; set;
    }
    public BaseReference BaseReference
    {
        get; set;
    }
    public int? EligibilityId
    {
        get; set;
    }
    public string? EligibilityName
    {
        get; set;
    }
}
public class CreateRulePercentageDto
{
    public decimal Percentage
    {
        get; set;
    }
    public BaseReference BaseReference
    {
        get; set;
    }
    public int? EligibilityId
    {
        get; set;
    }
}

public class RuleFormulaDto
{
    public int Id
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
    public string ParameterName { get; set; } = string.Empty; public CapUnit ResultUnit
    {
        get; set;
    }
    public decimal CurrentCapValue
    {
        get; set;
    }
}
public class CreateRuleFormulaDto
{
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
}

public class RuleDualCapDto
{
    public int Id
    {
        get; set;
    }
    public decimal FixedCapAmount
    {
        get; set;
    }
    public CapUnit FixedCapUnit
    {
        get; set;
    }
    public decimal PercentageCap
    {
        get; set;
    }
    public BaseReference BaseReference
    {
        get; set;
    }
    public DualCapLogic Logic
    {
        get; set;
    }
}

public class CreateRuleDualCapDto
{
    public decimal FixedCapAmount
    {
        get; set;
    }
    public CapUnit FixedCapUnit
    {
        get; set;
    }
    public decimal PercentageCap
    {
        get; set;
    }
    public BaseReference BaseReference
    {
        get; set;
    }
    public DualCapLogic Logic { get; set; } = DualCapLogic.MIN;
}

public class RuleTierDto
{
    public int Id
    {
        get; set;
    }
    public int TierOrder
    {
        get; set;
    }
    public decimal? MinAmount
    {
        get; set;
    }
    public decimal? MaxAmount
    {
        get; set;
    }
    public decimal ExemptionRate
    {
        get; set;
    }
}
public class CreateRuleTierDto
{
    public int TierOrder
    {
        get; set;
    }
    public decimal? MinAmount
    {
        get; set;
    }
    public decimal? MaxAmount
    {
        get; set;
    }
    public decimal ExemptionRate
    {
        get; set;
    }
}

public class RuleVariantDto
{
    public int Id
    {
        get; set;
    }
    public string VariantType { get; set; } = string.Empty;
    public string VariantKey { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public decimal? OverrideCap
    {
        get; set;
    }
    public int? OverrideEligibilityId
    {
        get; set;
    }
    public string? OverrideEligibilityName
    {
        get; set;
    }
}

public class CreateRuleVariantDto
{
    public string VariantType { get; set; } = "ZONE";
    public required string VariantKey
    {
        get; set;
    }
    public required string VariantLabel
    {
        get; set;
    }
    public decimal? OverrideCap
    {
        get; set;
    }
    public int? OverrideEligibilityId
    {
        get; set;
    }
}

// ════════════════════════════════════════════════════════════
// ANCIENNETÉ RATE SETS
// ════════════════════════════════════════════════════════════

public class AncienneteRateDto
{
    public int Id
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
}
public class CreateAncienneteRateDto
{
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
}

public class AncienneteRateSetDto
{
    public int Id
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
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
    public bool IsActive
    {
        get; set;
    }
    public int? CompanyId
    {
        get; set;
    }
    public int? ClonedFromId
    {
        get; set;
    }
    public List<AncienneteRateDto> Rates { get; set; } = new();
}

public class CreateAncienneteRateSetDto
{
    public required string Name
    {
        get; set;
    }
    public bool IsLegalDefault { get; set; } = true;
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
    public List<CreateAncienneteRateDto> Rates { get; set; } = new();
}

public class UpdateAncienneteRateSetDto
{
    public required string Name
    {
        get; set;
    }
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
    public List<CreateAncienneteRateDto> Rates { get; set; } = new();
}

public class UpdateAncienneteRatesDto
{
    public string? Name
    {
        get; set;
    }
    public List<CreateAncienneteRateDto> Rates { get; set; } = new();
}

public class CustomizeCompanyRatesDto
{
    public int CompanyId
    {
        get; set;
    }
    public List<CreateAncienneteRateDto> Rates { get; set; } = new();
}

public class RateValidationResultDto
{
    public bool IsValid
    {
        get; set;
    }
    public List<string> Violations { get; set; } = new();
}
