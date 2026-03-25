using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Payzen.Domain.Enums;

namespace Payzen.Application.DTOs.Company;

// ════════════════════════════════════════════════════════════
// COMPANY CREATE / UPDATE / READ
// ════════════════════════════════════════════════════════════

public class CompanyCreateDto
{
    [Required(ErrorMessage = "Le nom de l'entreprise est requis")]
    [StringLength(500, MinimumLength = 2)]
    public required string CompanyName { get; set; }

    [Required(ErrorMessage = "L'email professionnel est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [StringLength(500)]
    public required string CompanyEmail { get; set; }

    [Required(ErrorMessage = "Le numéro de téléphone est requis")]
    [StringLength(20)]
    public required string CompanyPhoneNumber { get; set; }

    [StringLength(10)]
    public string? CountryPhoneCode { get; set; }

    [Required(ErrorMessage = "L'adresse est requise")]
    [StringLength(1000)]
    public required string CompanyAddress { get; set; }

    [Required(ErrorMessage = "Le pays est requis")]
    public required int CountryId { get; set; }

    // Ville : existante OU nouvelle (saisie libre)
    public int? CityId { get; set; }

    [StringLength(500)]
    public string? CityName { get; set; }

    [Required(ErrorMessage = "Le numéro CNSS employeur est requis")]
    [StringLength(100)]
    public required string CnssNumber { get; set; }

    public bool IsCabinetExpert { get; set; } = false;

    [StringLength(100)]
    public string? IceNumber { get; set; }

    [StringLength(100)]
    public string? IfNumber { get; set; }

    [StringLength(100)]
    public string? RcNumber { get; set; }

    [StringLength(100)]
    public string? RibNumber { get; set; }

    [StringLength(50)]
    public string? LegalForm { get; set; }

    public DateTime? FoundingDate { get; set; }

    [StringLength(200)]
    public string? BusinessSector { get; set; }

    [StringLength(100)]
    public string? PatenteNumber { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "Format d'URL invalide")]
    public string? WebsiteUrl { get; set; }

    [StringLength(100)]
    public string? PaymentMethod { get; set; }

    // ─── Administrateur ──────────────────────────────────────

    [Required(ErrorMessage = "Le prénom de l'administrateur est requis")]
    [StringLength(100, MinimumLength = 2)]
    public required string AdminFirstName { get; set; }

    [Required(ErrorMessage = "Le nom de l'administrateur est requis")]
    [StringLength(100, MinimumLength = 2)]
    public required string AdminLastName { get; set; }

    [Required(ErrorMessage = "L'email de l'administrateur est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [StringLength(500)]
    public required string AdminEmail { get; set; }

    [Required(ErrorMessage = "Le téléphone de l'administrateur est requis")]
    [StringLength(20)]
    public required string AdminPhone { get; set; }

    public bool isActive { get; set; } = true;
}

/// <summary>
/// Création d'une entreprise cliente par un cabinet expert.
/// Ajoute ManagedByCompanyId obligatoire.
/// </summary>
public class CompanyCreateByExpertDto : CompanyCreateDto
{
    [Required(ErrorMessage = "Identifiant du cabinet expert requis")]
    public required int ManagedByCompanyId { get; set; }
}

/// <summary>Mise à jour partielle (PATCH) — tous les champs optionnels.</summary>
public class CompanyUpdateDto
{
    [StringLength(500, MinimumLength = 2)]
    public string? CompanyName { get; set; }

    [EmailAddress]
    [StringLength(500)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(10)]
    public string? CountryPhoneCode { get; set; }

    [StringLength(1000)]
    public string? CompanyAddress { get; set; }

    public int? CountryId { get; set; }
    public string? CountryName { get; set; } = string.Empty;
    public int? CityId { get; set; }

    [StringLength(500)]
    public string? CityName { get; set; }

    [StringLength(100)]
    public string? CnssNumber { get; set; }

    public bool? IsCabinetExpert { get; set; }

    [StringLength(100)]
    public string? IceNumber { get; set; }

    [StringLength(100)]
    public string? IfNumber { get; set; }

    [StringLength(100)]
    public string? RcNumber { get; set; }

    [StringLength(100)]
    public string? PatenteNumber { get; set; }

    [StringLength(100)]
    public string? RibNumber { get; set; }

    [StringLength(500)]
    public string? WebsiteUrl { get; set; }

    [StringLength(50)]
    public string? LegalForm { get; set; }

    public DateTime? FoundingDate { get; set; }
    public bool? IsActive { get; set; }

    public int? ManagedByCompanyId { get; set; }

    [StringLength(200)]
    public string? SignatoryName { get; set; }

    [StringLength(100)]
    public string? SignatoryTitle { get; set; }

    /// <summary>"Mensuelle" ou "Bimensuelle"</summary>
    [StringLength(50)]
    public string? PayrollPeriodicity { get; set; }

    // AuthType : "JWT" (backoffice/email+password) ou "C" (Microsoft Entra External ID, Type C).
    [StringLength(20)]
    public string? AuthType { get; set; }
}

/// <summary>Liste — retourné par GET /api/companies</summary>
public class CompanyListDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsCabinetExpert { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CountryPhoneCode { get; set; }
    public string? CityName { get; set; }
    public string? CountryName { get; set; }
    public string? CompanyAddress { get; set; } = string.Empty;
    public string CnssNumber { get; set; } = string.Empty;
    public string? IceNumber { get; set; }
    public string? IfNumber { get; set; }
    public string? RcNumber { get; set; }
    public string? PatenteNumber { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? RibNumber { get; set; }
    public string? LegalForm { get; set; }
    public bool? isActive { get; set; }
    public DateTime? FoundingDate { get; set; }
    public string? SignatoryName { get; set; }
    public string? SignatoryTitle { get; set; }
    public string? PayrollPeriodicity { get; set; }
    public string? AuthType { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Détail — retourné par GET /api/companies/{id}</summary>
public class CompanyReadDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CountryPhoneCode { get; set; }
    public string CompanyAddress { get; set; } = string.Empty;
    public int CityId { get; set; }
    public string? CityName { get; set; }
    public int CountryId { get; set; }
    public string? CountryName { get; set; }
    public string CnssNumber { get; set; } = string.Empty;
    public bool IsCabinetExpert { get; set; }
    public string? IceNumber { get; set; }
    public string? IfNumber { get; set; }
    public string? RcNumber { get; set; }
    public string? LegalForm { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? PatentNumber { get; set; }
    public string? RibNumber { get; set; }
    public DateTime? FoundingDate { get; set; }
    public string? BusinessSector { get; set; }
    public bool isActive { get; set; }
    public string? SignatoryName { get; set; }
    public string? SignatoryTitle { get; set; }
    public string? PayrollPeriodicity { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? AuthType { get; set; }
}

/// <summary>Réponse après création d'une entreprise (fiche employé admin + invitation e-mail Entra)</summary>
public class CompanyCreateResponseDto
{
    public CompanyReadDto Company { get; set; } = null!;
    public AdminAccountDto Admin { get; set; } = null!;
}

public class AdminAccountDto
{
    public int EmployeeId { get; set; }
    /// <summary>Null tant que l'admin n'a pas accepté l'invitation (connexion Entra).</summary>
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ════════════════════════════════════════════════════════════
// FORM DATA (pour alimenter les dropdowns du formulaire)
// ════════════════════════════════════════════════════════════

public class CompanyFormDataDto
{
    public List<CountryFormDto> Countries { get; set; } = new();
    public List<CityFormDto> Cities { get; set; } = new();
}

public class CountryFormDto
{
    public int Id { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public string? CountryNameAr { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string CountryPhoneCode { get; set; } = string.Empty;
}

public class CityFormDto
{
    public int Id { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public string? CountryName { get; set; }
}

// ════════════════════════════════════════════════════════════
// HISTORIQUE
// ════════════════════════════════════════════════════════════

public class CompanyHistoryDto
{
    public string Type { get; set; } = null!;        // "company" | "employee" | autre
    public string Title { get; set; } = null!;
    public string Date { get; set; } = null!;         // "2025-12-23"
    public string Description { get; set; } = null!;
    public Dictionary<string, object?>? Details { get; set; }
    public ModifiedByDto? ModifiedBy { get; set; }
    public string Timestamp { get; set; } = null!;    // ISO 8601
}

public class ModifiedByDto
{
    public string? Name { get; set; }
    public string? Role { get; set; }
}

// ════════════════════════════════════════════════════════════
// DEPARTEMENT
// ════════════════════════════════════════════════════════════

public class DepartementCreateDto
{
    [Required(ErrorMessage = "Le nom du département est requis")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom du département doit contenir entre 2 et 500 caractères")]
    public required string DepartementName { get; set; }

    [Required(ErrorMessage = "L'ID de la société est requis")]
    public required int CompanyId { get; set; }
}

public class DepartementUpdateDto
{
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom du département doit contenir entre 2 et 500 caractères")]
    public string? DepartementName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")]
    public int? CompanyId { get; set; }
}

public class DepartementReadDto
{
    public int Id { get; set; }
    public string DepartementName { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// JOB POSITION
// ════════════════════════════════════════════════════════════

public class JobPositionCreateDto
{
    [Required(ErrorMessage = "Le nom du poste est requis")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom du poste doit contenir entre 2 et 200 caractères")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "L'identifiant de la société est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'identifiant de la société doit être valide")]
    public int CompanyId { get; set; }
}

public class JobPositionUpdateDto
{
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom du poste doit contenir entre 2 et 200 caractères")]
    public string? Name { get; set; }
}

public class JobPositionReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// CONTRACT TYPE
// ════════════════════════════════════════════════════════════

public class ContractTypeCreateDto
{
    [Required(ErrorMessage = "Le nom du type de contrat est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom du type de contrat doit contenir entre 2 et 100 caractères")]
    public required string ContractTypeName { get; set; }

    [Required(ErrorMessage = "L'identifiant de la société est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'identifiant de la société doit être valide")]
    public int CompanyId { get; set; }

    public int? LegalContractTypeId { get; set; }
    public int? StateEmploymentProgramId { get; set; }
}

public class ContractTypeUpdateDto
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom du type de contrat doit contenir entre 2 et 100 caractères")]
    public string? ContractTypeName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "L'identifiant du type de contrat légal doit être valide")]
    public int? LegalContractTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "L'identifiant du programme d'emploi d'état doit être valide")]
    public int? StateEmploymentProgramId { get; set; }
}

public class ContractTypeReadDto
{
    public int Id { get; set; }
    public string ContractTypeName { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? LegalContractTypeId { get; set; }
    public string? LegalContractTypeName { get; set; }
    public int? StateEmploymentProgramId { get; set; }
    public string? StateEmploymentProgramName { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// HOLIDAY (multilingue FR/AR/EN)
// ════════════════════════════════════════════════════════════

public class HolidayCreateDto
{
    [Required(ErrorMessage = "Le nom en français est requis")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caractères")]
    public required string NameFr { get; set; }

    [Required(ErrorMessage = "Le nom en arabe est requis")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caractères")]
    public required string NameAr { get; set; }

    [Required(ErrorMessage = "Le nom en anglais est requis")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caractères")]
    public required string NameEn { get; set; }

    [Required(ErrorMessage = "La date du jour férié est requise")]
    public required DateOnly HolidayDate { get; set; }

    [StringLength(1000, ErrorMessage = "La description ne peut pas dépasser 1000 caractères")]
    public string? Description { get; set; }

    public int? CompanyId { get; set; }  // null = Global (niveau pays)

    [Required(ErrorMessage = "L'ID du pays est requis")]
    public int CountryId { get; set; }

    [Required(ErrorMessage = "Le scope est requis")]
    public HolidayScope Scope { get; set; } = HolidayScope.Global;

    [Required(ErrorMessage = "Le type de jour férié est requis")]
    [StringLength(50, ErrorMessage = "Le type ne peut pas dépasser 50 caractères")]
    public required string HolidayType { get; set; }  // National, Religieux, Company, etc.

    public bool IsMandatory { get; set; } = true;
    public bool IsPaid { get; set; } = true;
    public bool IsRecurring { get; set; } = false;

    [StringLength(500, ErrorMessage = "La règle de récurrence ne peut pas dépasser 500 caractères")]
    public string? RecurrenceRule { get; set; }

    [Range(2020, 2100, ErrorMessage = "L'année doit être entre 2020 et 2100")]
    public int? Year { get; set; }

    public bool AffectPayroll { get; set; } = true;
    public bool AffectAttendance { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class HolidayUpdateDto
{
    [StringLength(200, MinimumLength = 2)]
    public string? NameFr { get; set; }

    [StringLength(200, MinimumLength = 2)]
    public string? NameAr { get; set; }

    [StringLength(200, MinimumLength = 2)]
    public string? NameEn { get; set; }

    public DateOnly? HolidayDate { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public int? CountryId { get; set; }
    public HolidayScope? Scope { get; set; }

    [StringLength(50)]
    public string? HolidayType { get; set; }

    public bool? IsMandatory { get; set; }
    public bool? IsPaid { get; set; }
    public bool? IsRecurring { get; set; }

    [StringLength(500)]
    public string? RecurrenceRule { get; set; }

    [Range(2020, 2100)]
    public int? Year { get; set; }

    public bool? AffectPayroll { get; set; }
    public bool? AffectAttendance { get; set; }
    public bool? IsActive { get; set; }
}

public class HolidayReadDto
{
    public int Id { get; set; }
    public string NameFr { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public DateOnly HolidayDate { get; set; }
    public string? Description { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public HolidayScope Scope { get; set; }
    public string ScopeDescription { get; set; } = string.Empty;
    public string HolidayType { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public bool IsPaid { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public int? Year { get; set; }
    public bool AffectPayroll { get; set; }
    public bool AffectAttendance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// WORKING CALENDAR
// ════════════════════════════════════════════════════════════

public class WorkingCalendarCreateDto
{
    [Required(ErrorMessage = "L'ID de la société est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")]
    public required int CompanyId { get; set; }

    [Required(ErrorMessage = "Le jour de la semaine est requis")]
    [Range(0, 6, ErrorMessage = "Le jour doit être entre 0 (Dimanche) et 6 (Samedi)")]
    public required int DayOfWeek { get; set; }

    [Required(ErrorMessage = "Le statut jour travaillé est requis")]
    public required bool IsWorkingDay { get; set; }

    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}

public class WorkingCalendarUpdateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")]
    public int? CompanyId { get; set; }

    [Range(0, 6, ErrorMessage = "Le jour doit être entre 0 (Dimanche) et 6 (Samedi)")]
    public int? DayOfWeek { get; set; }

    public bool? IsWorkingDay { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}

public class WorkingCalendarReadDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; } = string.Empty;  // "Lundi", "Mardi", etc.
    public bool IsWorkingDay { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// COMPANY DOCUMENT
// ════════════════════════════════════════════════════════════

public class CompanyDocumentCreateDto
{
    [Required(ErrorMessage = "L'ID de l'entreprise est requis")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Le nom du document est requis")]
    [StringLength(500, ErrorMessage = "Le nom ne peut pas dépasser 500 caractères")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le chemin du fichier est requis")]
    [StringLength(1000, ErrorMessage = "Le chemin ne peut pas dépasser 1000 caractères")]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Le type ne peut pas dépasser 100 caractères")]
    public string? DocumentType { get; set; }
}

public class CompanyDocumentUpdateDto
{
    [StringLength(500, ErrorMessage = "Le nom ne peut pas dépasser 500 caractères")]
    public string? Name { get; set; }

    [StringLength(100, ErrorMessage = "Le type ne peut pas dépasser 100 caractères")]
    public string? DocumentType { get; set; }
}

public class CompanyDocumentUploadDto
{
    [Required(ErrorMessage = "L'ID de l'entreprise est requis")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Le fichier est requis")]
    public IFormFile File { get; set; } = null!;

    [StringLength(100, ErrorMessage = "Le type ne peut pas dépasser 100 caractères")]
    public string? DocumentType { get; set; }
}

public class CompanyDocumentReadDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public DateTime CreatedAt { get; set; }
}