using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Payzen.Domain.Enums;

namespace Payzen.Application.DTOs.Employee;

// ════════════════════════════════════════════════════════════
// EMPLOYEE (core)
// ════════════════════════════════════════════════════════════

public class EmployeeCreateDto
{
    // ── Obligatoires ──────────────────────────────────────────
    [Required(ErrorMessage = "Le prénom est requis")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le prénom doit contenir entre 2 et 500 caractères")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Le nom de famille est requis")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom de famille doit contenir entre 2 et 500 caractères")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "Le numéro CIN est requis")]
    [StringLength(500, ErrorMessage = "Le numéro CIN ne peut pas dépasser 500 caractères")]
    public required string CinNumber { get; set; }

    [Required(ErrorMessage = "La date de naissance est requise")]
    public required DateOnly DateOfBirth { get; set; }

    [Required(ErrorMessage = "Le numéro de téléphone est requis")]
    [RegularExpression("^\\d{9}$", ErrorMessage = "Le numéro de téléphone doit contenir exactement 9 chiffres")]
    public required string Phone { get; set; }

    [Required(ErrorMessage = "L'indicatif pays est requis")]
    [StringLength(10, ErrorMessage = "Le code téléphonique ne peut pas dépasser 10 caractères")]
    [RegularExpression("^\\+\\d{1,4}$", ErrorMessage = "L'indicatif pays est invalide (ex: +212)")]
    public required string CountryPhoneCode { get; set; }

    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [StringLength(500, ErrorMessage = "L'email ne peut pas dépasser 500 caractères")]
    public required string Email { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")]
    public int? CompanyId { get; set; }

    [Required(ErrorMessage = "L'ID du statut est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du statut doit être valide")]
    public required int StatusId { get; set; }

    // ── Optionnelles ──────────────────────────────────────────
    public int? DepartementId { get; set; }
    public int? ManagerId { get; set; }
    public int? GenderId { get; set; }
    public int? NationalityId { get; set; }
    public int? EducationLevelId { get; set; }
    public int? MaritalStatusId { get; set; }
    public string? CnssNumber { get; set; }
    public string? CimrNumber { get; set; }
    public decimal? CimrEmployeeRate { get; set; }
    public decimal? CimrCompanyRate { get; set; }
    public bool? HasPrivateInsurance { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "L'ID de la catégorie doit être valide")]
    public int? CategoryId { get; set; }

    // ── Adresse (optionnelle) ─────────────────────────────────
    public int? CountryId { get; set; }
    public int? CityId { get; set; }

    [StringLength(500, ErrorMessage = "L'adresse ligne 1 ne peut pas dépasser 500 caractères")]
    public string? AddressLine1 { get; set; }

    [StringLength(500, ErrorMessage = "L'adresse ligne 2 ne peut pas dépasser 500 caractères")]
    public string? AddressLine2 { get; set; }

    [StringLength(20, ErrorMessage = "Le code postal ne peut pas dépasser 20 caractères")]
    public string? ZipCode { get; set; } = string.Empty;

    // ── Contrat (optionnel) ───────────────────────────────────
    public int? JobPositionId { get; set; }
    public int? ContractTypeId { get; set; }
    public DateTime? StartDate { get; set; }

    // ── Salaire (optionnel) ───────────────────────────────────
    [Range(0, double.MaxValue, ErrorMessage = "Le salaire doit être positif")]
    public decimal? Salary { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Le salaire horaire doit être positif")]
    public decimal? SalaryHourly { get; set; }

    [JsonPropertyName("annualLeave")]
    [Range(0, double.MaxValue, ErrorMessage = "Le solde initial des congés annuels doit être positif")]
    public decimal? AnnualLeave { get; set; }

    /// <summary>
    /// Date d'effet du salaire.
    /// Si non renseignée, la date de début de contrat (StartDate) est utilisée.
    /// </summary>
    public DateTime? SalaryEffectiveDate { get; set; }

    // ── Compte utilisateur ────────────────────────────────────
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
    public string? Password { get; set; }

    // ── Rôle utilisé pour créer directement le compte employé (Entra + DB) ──
    public int? InviteRoleId { get; set; }

    public bool CreateUserAccount { get; set; } = true;
}

public class EmployeeUpdateDto
{
    // ── Données principales ───────────────────────────────────
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le prénom doit contenir entre 2 et 500 caractères")]
    public string? FirstName { get; set; }

    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom de famille doit contenir entre 2 et 500 caractères")]
    public string? LastName { get; set; }

    [StringLength(500, ErrorMessage = "Le numéro CIN ne peut pas dépasser 500 caractères")]
    public string? CinNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public string? Phone { get; set; }

    [StringLength(10, ErrorMessage = "Le code téléphonique ne peut pas dépasser 10 caractères")]
    [RegularExpression("^\\+\\d{1,4}$", ErrorMessage = "L'indicatif pays est invalide (ex: +212)")]
    public string? CountryPhoneCode { get; set; }

    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [StringLength(500, ErrorMessage = "L'email ne peut pas dépasser 500 caractères")]
    public string? Email { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "L'ID du département doit être valide")]
    public int? DepartementId { get; set; }

    public int? ManagerId { get; set; }
    public DateTime? ManagerChangeDate { get; set; }
    public int? StatusId { get; set; }
    public int? GenderId { get; set; }
    public int? NationalityId { get; set; }
    public int? EducationLevelId { get; set; }
    public int? MaritalStatusId { get; set; }
    public DateTime? MaritalStatusChangeDate { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? CategoryChangeDate { get; set; }

    /// <summary>Numéro CNSS (chaîne). Clé JSON <c>cnss</c> pour alignement Angular.</summary>
    [JsonPropertyName("cnss")]
    public string? CnssNumber { get; set; }

    /// <summary>Numéro CIMR (chaîne). Clé JSON <c>cimr</c> pour alignement Angular.</summary>
    [JsonPropertyName("cimr")]
    public string? CimrNumber { get; set; }

    public decimal? CimrEmployeeRate { get; set; }
    public decimal? CimrCompanyRate { get; set; }
    public DateTime? CimrRatesChangeDate { get; set; }
    public bool? HasPrivateInsurance { get; set; }
    public DateTime? PrivateInsuranceChangeDate { get; set; }
    public string? PrivateInsuranceNumber { get; set; }
    public decimal? PrivateInsuranceRate { get; set; }
    public bool? DisableAmo { get; set; }

    /// <summary>Mode de paiement (ex. virement) — aligné sur <c>Employee.PaymentMethod</c>.</summary>
    public string? PaymentMethod { get; set; }

    // ── Avec historique (crée un nouveau record et ferme l'ancien) ──
    public int? JobPositionId { get; set; }
    public int? ContractTypeId { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractChangeDate { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Le salaire doit être supérieur à 0")]
    public decimal? Salary { get; set; }

    /// <summary>Salaire horaire actif. Clé JSON <c>baseSalaryHourly</c> (patch profil Angular).</summary>
    [JsonPropertyName("baseSalaryHourly")]
    public decimal? SalaryHourly { get; set; }

    [JsonPropertyName("annualLeave")]
    [Range(0, double.MaxValue, ErrorMessage = "Le solde initial des congés annuels doit être positif")]
    public decimal? AnnualLeave { get; set; }

    public DateTime? SalaryEffectiveDate { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? ZipCode { get; set; }
    public int? CityId { get; set; }
}

public class EmployeeReadDto
{
    public int Id { get; set; }
    public int? Matricule { get; set; }
    public string? MatriculeDisplay { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CinNumber { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int? DepartementId { get; set; }
    public string? DepartementName { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public int? StatusId { get; set; }
    public string? StatusName { get; set; } = string.Empty;
    public int? GenderId { get; set; }
    public int? NationalityId { get; set; }
    public int? EducationLevelId { get; set; }
    public int? MaritalStatusId { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CnssNumber { get; set; }
    public string? CimrNumber { get; set; }
    public string? CimrEmployeeRate { get; set; }
    public string? CimrCompanyRate { get; set; }
    public bool HasPrivateInsurance { get; set; } = false;
    public bool DisableAmo { get; set; } = false;
    public string? PrivateInsuranceNumber { get; set; }
    public decimal? PrivateInsuranceRate { get; set; }
    public string? JobPostionName { get; set; } = string.Empty; // note: typo conservée volontairement (source)
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Vue détaillée d'un employé — inclut contrat actif, salaire, composants, adresse, cotisations.
/// Retourné par GET /api/employees/{id}/detail
/// </summary>
public class EmployeeDetailDto
{
    public int Id { get; set; }
    public int? Matricule { get; set; }
    public string? MatriculeDisplay { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CinNumber { get; set; } = string.Empty;
    public string? MaritalStatusName { get; set; }
    [JsonPropertyName("maritalStatusChangeDate")]
    public DateTime? MaritalStatusChangeDate { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string? StatusName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string? CountryPhoneCode { get; set; }
    public int? GenderId { get; set; }
    public string? GenderName { get; set; }

    // Adresse
    public EmployeeAddressDto? Address { get; set; }

    // Contrat actif
    public int? JobPositionId { get; set; }
    public string? JobPositionName { get; set; }
    public int? ContractTypeId { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    [JsonPropertyName("managerChangeDate")]
    public DateTime? ManagerChangeDate { get; set; }
    public DateTime? ContractStartDate { get; set; }
    [JsonPropertyName("contractChangeDate")]
    public DateTime? ContractChangeDate { get; set; }
    public string? ContractTypeName { get; set; }
    public int? DepartementId { get; set; }
    public string? departments { get; set; }

    // Salaire
    public decimal? BaseSalary { get; set; }
    public decimal? BaseSalaryHourly { get; set; }

    /// <summary>Date d'effet de l'enregistrement salarial actif (<c>EmployeeSalary.EffectiveDate</c>).</summary>
    [JsonPropertyName("salaryEffectiveDate")]
    public DateTime? SalaryEffectiveDate { get; set; }

    public List<SalaryComponentDto> SalaryComponents { get; set; } = new();
    public decimal TotalSalary { get; set; }

    [JsonPropertyName("salaryPaymentMethod")]
    public string? SalaryPaymentMethod { get; set; }

    // Cotisations (camelCase conservé du source)
    public string? cnss { get; set; }
    public string? cimr { get; set; }
    public decimal? cimrEmployeeRate { get; set; }
    public decimal? cimrCompanyRate { get; set; }
    [JsonPropertyName("cimrRatesChangeDate")]
    public DateTime? CimrRatesChangeDate { get; set; }
    public bool? hasPrivateInsurance { get; set; }
    [JsonPropertyName("privateInsuranceChangeDate")]
    public DateTime? PrivateInsuranceChangeDate { get; set; }
    public string? privateInsuranceNumber { get; set; }
    public decimal? privateInsuranceRate { get; set; }
    public bool disableAmo { get; set; }
    public decimal? annualLeave { get; set; }
    [JsonPropertyName("categoryChangeDate")]
    public DateTime? CategoryChangeDate { get; set; }

    // Événements
    public List<EmployeeDetailHistoryEventDto> Events { get; set; } = new();

    public DateTime CreatedAt { get; set; }
}

public class EmployeeAddressDto
{
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string ZipCode { get; set; } = string.Empty;
    public int? CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int? CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
}

public class SalaryComponentDto
{
    public string ComponentName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; }
}

public class EmployeeDetailHistoryModifierDto
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>Détails sensibles d'un événement d'historique (rempli uniquement pour le rôle RH).</summary>
public class EmployeeHistoryEventDetailsDto
{
    public string? OldSalary { get; set; }
    public string? NewSalary { get; set; }
    public string? Currency { get; set; }
    public string? OldPosition { get; set; }
    public string? NewPosition { get; set; }
    public string? ContractInfo { get; set; }
    public string? EndDate { get; set; }
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? OldDepartment { get; set; }
    public string? NewDepartment { get; set; }
    public string? OldManager { get; set; }
    public string? NewManager { get; set; }
    public string? OldAddress { get; set; }
    public string? NewAddress { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public class EmployeeDetailHistoryEventDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EmployeeHistoryEventDetailsDto? Details { get; set; }
    public EmployeeDetailHistoryModifierDto ModifiedBy { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>DTO simplifié pour la liste des employés actifs (dropdowns, managers, etc.)</summary>
public class EmployeeSimpleDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<string>? RoleNames { get; set; }
    public string? statuses { get; set; }
}

// ════════════════════════════════════════════════════════════
// FORM DATA (pour alimenter les dropdowns)
// ════════════════════════════════════════════════════════════

public class EmployeeFormDataDto
{
    public List<StatusFormDto> Statuses { get; set; } = new();
    public List<GenderFormDto> Genders { get; set; } = new();
    public List<EducationLevelFormDto> EducationLevels { get; set; } = new();
    public List<MaritalStatusFormDto> MaritalStatuses { get; set; } = new();
    public List<NationalityDto> Nationalities { get; set; } = new();
    public List<CountryDto> Countries { get; set; } = new();
    public List<CityDto> Cities { get; set; } = new();
    public List<DepartementDto> Departements { get; set; } = new();
    public List<JobPositionDto> JobPositions { get; set; } = new();
    public List<ContractTypeDto> ContractTypes { get; set; } = new();
    public List<EmployeeDto> PotentialManagers { get; set; } = new();
    public List<EmployeeCategorySimpleDto> EmployeeCategories { get; set; } = new();
}

public class CountryDto
{
    public int Id { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public string? CountryPhoneCode { get; set; }
}

public class CityDto
{
    public int Id { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public string? CountryName { get; set; }
}

public class DepartementDto
{
    public int Id { get; set; }
    public string DepartementName { get; set; } = string.Empty;
    public int CompanyId { get; set; }
}

public class JobPositionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CompanyId { get; set; }
}

public class ContractTypeDto
{
    public int Id { get; set; }
    public string ContractTypeName { get; set; } = string.Empty;
    public int CompanyId { get; set; }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DepartementName { get; set; }
}

public class NationalityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class StatusFormDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class GenderFormDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class EducationLevelFormDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MaritalStatusFormDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class EmployeeCategorySimpleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public EmployeeCategoryMode Mode { get; set; }
    public string PayrollPeriodicity { get; set; } = "Mensuelle";
}

// ════════════════════════════════════════════════════════════
// CONTRACT
// ════════════════════════════════════════════════════════════

public class EmployeeContractCreateDto
{
    [Required(ErrorMessage = "L'ID de l'employé est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'employé doit être valide")]
    public required int EmployeeId { get; set; }

    [Required(ErrorMessage = "L'ID de la société est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")]
    public required int CompanyId { get; set; }

    [Required(ErrorMessage = "L'ID du poste est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du poste doit être valide")]
    public required int JobPositionId { get; set; }

    [Required(ErrorMessage = "L'ID du type de contrat est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du type de contrat doit être valide")]
    public required int ContractTypeId { get; set; }

    [Required(ErrorMessage = "La date de début est requise")]
    public required DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

public class EmployeeContractUpdateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du poste doit être valide")]
    public int? JobPositionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "L'ID du type de contrat doit être valide")]
    public int? ContractTypeId { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class EmployeeContractReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int JobPositionId { get; set; }
    public string JobPositionName { get; set; } = string.Empty;
    public int ContractTypeId { get; set; }
    public string ContractTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// SALARY
// ════════════════════════════════════════════════════════════

public class EmployeeSalaryCreateDto
{
    [Required(ErrorMessage = "L'ID de l'employé est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'employé doit être valide")]
    public required int EmployeeId { get; set; }

    [Required(ErrorMessage = "L'ID du contrat est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du contrat doit être valide")]
    public required int ContractId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Le salaire doit être supérieur à 0")]
    public decimal? BaseSalary { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Le salaire horaire doit être supérieur à 0")]
    public decimal? BaseSalaryHourly { get; set; }

    [Required(ErrorMessage = "La date d'effet est requise")]
    public required DateTime EffectiveDate { get; set; }

    public DateTime? EndDate { get; set; }
}

public class EmployeeSalaryUpdateDto
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Le salaire doit être supérieur à 0")]
    public decimal? BaseSalary { get; set; }
    public decimal? BaseSalaryHourly { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class EmployeeSalaryReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public int ContractId { get; set; }
    public decimal? BaseSalary { get; set; }
    public decimal? BaseSalaryHourly { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// SALARY COMPONENT
// ════════════════════════════════════════════════════════════

public class EmployeeSalaryComponentCreateDto
{
    [Required(ErrorMessage = "L'ID du salaire est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du salaire doit être valide")]
    public required int EmployeeSalaryId { get; set; }

    [Required(ErrorMessage = "Le type de composant est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le type doit contenir entre 2 et 100 caractères")]
    public required string ComponentType { get; set; }

    [Required(ErrorMessage = "Le montant est requis")]
    public required decimal Amount { get; set; }

    public bool IsTaxable { get; set; } = true;

    [Required(ErrorMessage = "La date d'effet est requise")]
    public required DateTime EffectiveDate { get; set; }

    public DateTime? EndDate { get; set; }
}

public class EmployeeSalaryComponentUpdateDto
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le type doit contenir entre 2 et 100 caractères")]
    public string? ComponentType { get; set; }
    public decimal? Amount { get; set; }
    public bool? IsTaxable { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class EmployeeSalaryComponentReadDto
{
    public int Id { get; set; }
    public int EmployeeSalaryId { get; set; }
    public string ComponentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// ADDRESS
// ════════════════════════════════════════════════════════════

public class EmployeeAddressCreateDto
{
    [Required(ErrorMessage = "L'ID de l'employé est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'employé doit être valide")]
    public required int EmployeeId { get; set; }

    [Required(ErrorMessage = "L'adresse ligne 1 est requise")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "L'adresse doit contenir entre 5 et 500 caractères")]
    public required string AddressLine1 { get; set; }

    [StringLength(500, ErrorMessage = "L'adresse ligne 2 ne peut pas dépasser 500 caractères")]
    public string? AddressLine2 { get; set; }

    [Required(ErrorMessage = "Le code postal est requis")]
    [StringLength(20, MinimumLength = 4, ErrorMessage = "Le code postal doit contenir entre 4 et 20 caractères")]
    public required string ZipCode { get; set; }

    [Required(ErrorMessage = "L'ID de la ville est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de la ville doit être valide")]
    public required int CityId { get; set; }

    [Required(ErrorMessage = "L'ID du pays est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du pays doit être valide")]
    public required int CountryId { get; set; }
}

public class EmployeeAddressUpdateDto
{
    [StringLength(500, MinimumLength = 5, ErrorMessage = "L'adresse doit contenir entre 5 et 500 caractères")]
    public string? AddressLine1 { get; set; }

    [StringLength(500, ErrorMessage = "L'adresse ligne 2 ne peut pas dépasser 500 caractères")]
    public string? AddressLine2 { get; set; }

    [StringLength(20, MinimumLength = 4, ErrorMessage = "Le code postal doit contenir entre 4 et 20 caractères")]
    public string? ZipCode { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "L'ID de la ville doit être valide")]
    public int? CityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "L'ID du pays doit être valide")]
    public int? CountryId { get; set; }
}

public class EmployeeAddressReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string ZipCode { get; set; } = string.Empty;
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// DOCUMENT
// ════════════════════════════════════════════════════════════

public class EmployeeDocumentCreateDto
{
    [Required(ErrorMessage = "L'ID de l'employé est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'employé doit être valide")]
    public required int EmployeeId { get; set; }

    [Required(ErrorMessage = "Le nom du document est requis")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caractères")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Le chemin du fichier est requis")]
    [StringLength(1000, ErrorMessage = "Le chemin ne peut pas dépasser 1000 caractères")]
    public required string FilePath { get; set; }

    public DateTime? ExpirationDate { get; set; }

    [Required(ErrorMessage = "Le type de document est requis")]
    [StringLength(100, ErrorMessage = "Le type ne peut pas dépasser 100 caractères")]
    public required string DocumentType { get; set; }
}

public class EmployeeDocumentUpdateDto
{
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caractères")]
    public string? Name { get; set; }

    [StringLength(1000, ErrorMessage = "Le chemin ne peut pas dépasser 1000 caractères")]
    public string? FilePath { get; set; }

    public DateTime? ExpirationDate { get; set; }

    [StringLength(100, ErrorMessage = "Le type ne peut pas dépasser 100 caractères")]
    public string? DocumentType { get; set; }
}

public class EmployeeDocumentReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// CHILD
// ════════════════════════════════════════════════════════════

public class EmployeeChildCreateDto
{
    /// <summary>Renseigné par la route (<c>.../employee/{id}/children</c>) — ne pas exiger dans le JSON.</summary>
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Le prénom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le prénom doit contenir entre 2 et 100 caractères")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "La date de naissance est requise")]
    public required DateTime DateOfBirth { get; set; }

    public int? GenderId { get; set; }
    public bool IsDependent { get; set; } = true;
    public bool IsStudent { get; set; } = false;
}

public class EmployeeChildUpdateDto
{
    [Required(ErrorMessage = "Le prénom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le prénom doit contenir entre 2 et 100 caractères")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "La date de naissance est requise")]
    public required DateTime DateOfBirth { get; set; }

    public int? GenderId { get; set; }
    public bool IsDependent { get; set; }
    public bool IsStudent { get; set; }
}

public class EmployeeChildReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeFirstName { get; set; } = string.Empty;
    public string EmployeeLastName { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public int Age { get; set; }
    public int? GenderId { get; set; }
    public string? GenderName { get; set; }
    public bool IsDependent { get; set; }
    public bool IsStudent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// SPOUSE
// ════════════════════════════════════════════════════════════

public class EmployeeSpouseCreateDto
{
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Le prénom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le prénom doit contenir entre 2 et 100 caractères")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "La date de naissance est requise")]
    public required DateTime DateOfBirth { get; set; }

    public int? GenderId { get; set; }

    [StringLength(50, ErrorMessage = "Le numéro CIN ne peut pas dépasser 50 caractères")]
    public string? CinNumber { get; set; }

    public DateTime? MarriageDate { get; set; }
    public bool IsDependent { get; set; } = false;
}

public class EmployeeSpouseUpdateDto
{
    [Required(ErrorMessage = "Le prénom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le prénom doit contenir entre 2 et 100 caractères")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "La date de naissance est requise")]
    public required DateTime DateOfBirth { get; set; }

    public int? GenderId { get; set; }

    [StringLength(50, ErrorMessage = "Le numéro CIN ne peut pas dépasser 50 caractères")]
    public string? CinNumber { get; set; }

    public bool IsDependent { get; set; }
}

public class EmployeeSpouseReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeFirstName { get; set; } = string.Empty;
    public string EmployeeLastName { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public int Age { get; set; }
    public int? GenderId { get; set; }
    public string? GenderName { get; set; }
    public string? CinNumber { get; set; }
    public DateTime? MarriageDate { get; set; }
    public bool IsDependent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// ABSENCE
// ════════════════════════════════════════════════════════════

public class EmployeeAbsenceCreateDto
{
    [Required(ErrorMessage = "L'ID de l'employé est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'employé doit être valide")]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "La date d'absence est requise")]
    public DateOnly AbsenceDate { get; set; }

    [Required(ErrorMessage = "Le type de durée est requis")]
    public AbsenceDurationType DurationType { get; set; }

    public bool? IsMorning { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    [Required(ErrorMessage = "Le type d'absence est requis")]
    [StringLength(50, ErrorMessage = "Le type d'absence ne peut pas dépasser 50 caractères")]
    public required string AbsenceType { get; set; }

    [StringLength(500, ErrorMessage = "La raison ne peut pas dépasser 500 caractères")]
    public string? Reason { get; set; }

    /// <summary>Statut initial (ex. brouillon). Sinon comportement serveur (RH = approuvé, sinon brouillon ou soumis).</summary>
    public AbsenceStatus? Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class EmployeeAbsenceUpdateDto
{
    public DateOnly? AbsenceDate { get; set; }
    public AbsenceDurationType? DurationType { get; set; }
    public bool? IsMorning { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    [StringLength(50, ErrorMessage = "Le type d'absence ne peut pas dépasser 50 caractères")]
    public string? AbsenceType { get; set; }

    [StringLength(500, ErrorMessage = "La raison ne peut pas dépasser 500 caractères")]
    public string? Reason { get; set; }
}

public class EmployeeAbsenceDecisionDto
{
    [Required(ErrorMessage = "Le statut de décision est requis")]
    public AbsenceStatus Status { get; set; }

    [StringLength(1000, ErrorMessage = "Le commentaire ne peut pas dépasser 1000 caractères")]
    public string? DecisionComment { get; set; }
}

public class EmployeeAbsenceApprovalDto
{
    [StringLength(1000, ErrorMessage = "Le commentaire ne peut pas dépasser 1000 caractères")]
    public string? Comment { get; set; }
}

public class EmployeeAbsenceRejectionDto
{
    [Required(ErrorMessage = "La raison du rejet est requise")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "La raison doit contenir entre 3 et 1000 caractères")]
    public required string Reason { get; set; }
}

public class EmployeeAbsenceCancellationDto
{
    [StringLength(500, ErrorMessage = "La raison ne peut pas dépasser 500 caractères")]
    public string? Reason { get; set; }
}

public class EmployeeAbsenceReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeFirstName { get; set; } = string.Empty;
    public string EmployeeLastName { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;
    public DateOnly AbsenceDate { get; set; }
    public string AbsenceDateFormatted { get; set; } = string.Empty;
    public AbsenceDurationType DurationType { get; set; }
    public string DurationTypeDescription { get; set; } = string.Empty;
    public bool? IsMorning { get; set; }
    public string? HalfDayDescription { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string AbsenceType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public AbsenceStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public DateTime? DecisionAt { get; set; }
    public int? DecisionBy { get; set; }
    public string? DecisionByName { get; set; }
    public string? DecisionComment { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
}

public class EmployeeAbsenceStatsDto
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
    public string? EmployeeFullName { get; set; }
    public int TotalAbsences { get; set; }
    public int FullDayAbsences { get; set; }
    public int HalfDayAbsences { get; set; }
    public int HourlyAbsences { get; set; }
    public int SubmittedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int CancelledCount { get; set; }
    public Dictionary<string, int> AbsencesByType { get; set; } = new();
    public Dictionary<string, int> AbsencesByMonth { get; set; } = new();
    public DateTimeOffset GeneratedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// OVERTIME
// ════════════════════════════════════════════════════════════

public class EmployeeOvertimeCreateDto
{
    [Required(ErrorMessage = "L'ID de l'employé est requis")]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "La date est requise")]
    public required DateOnly OvertimeDate { get; set; }

    [Required(ErrorMessage = "Le mode de saisie est requis")]
    public OvertimeEntryMode EntryMode { get; set; }

    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    [Range(0.01, 24.00, ErrorMessage = "La durée doit être entre 0.01 et 24 heures")]
    public decimal? DurationInHours { get; set; }

    [Range(1.00, 24.00)]
    public decimal? StandardDayHours { get; set; }

    [StringLength(500)]
    public string? EmployeeComment { get; set; }
}

public class EmployeeOvertimeUpdateDto
{
    public DateOnly? OvertimeDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    [Range(0.01, 24.00)]
    public decimal? DurationInHours { get; set; }

    [StringLength(500)]
    public string? EmployeeComment { get; set; }
}

public class EmployeeOvertimeSubmitDto
{
    [StringLength(500)]
    public string? EmployeeComment { get; set; }
}

public class EmployeeOvertimeApprovalDto
{
    [Required(ErrorMessage = "La décision est requise")]
    public OvertimeStatus Status { get; set; } // Approved ou Rejected

    [StringLength(500)]
    public string? ManagerComment { get; set; }
}

/// <summary>Résultat POST création HS : une ligne ou plusieurs segments (split jour/nuit).</summary>
public class EmployeeOvertimeCreateOutcomeDto
{
    public IReadOnlyList<EmployeeOvertimeReadDto> Overtimes { get; init; } = Array.Empty<EmployeeOvertimeReadDto>();
    public Guid? SplitBatchId { get; init; }
}

public class EmployeeOvertimeReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public OvertimeType OvertimeType { get; set; }
    public string OvertimeTypeDescription { get; set; } = string.Empty;
    public OvertimeEntryMode EntryMode { get; set; }
    public int? HolidayId { get; set; }
    public string? HolidayName { get; set; }
    public DateOnly OvertimeDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool CrossesMidnight { get; set; }
    public decimal DurationInHours { get; set; }
    public decimal? StandardDayHours { get; set; }

    // Règle appliquée (snapshot)
    public int? RateRuleId { get; set; }
    public string? RateRuleCodeApplied { get; set; }
    public string? RateRuleNameApplied { get; set; }
    public decimal RateMultiplierApplied { get; set; }
    public string? MultiplierCalculationDetails { get; set; }

    // Split automatique
    public Guid? SplitBatchId { get; set; }
    public int? SplitSequence { get; set; }
    public int? SplitTotalSegments { get; set; }

    // Workflow
    public OvertimeStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public string? EmployeeComment { get; set; }
    public string? ManagerComment { get; set; }
    public int? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Paie
    public bool IsProcessedInPayroll { get; set; }
    public int? PayrollBatchId { get; set; }
    public DateTime? ProcessedInPayrollAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>Statistiques HS pour le frontend (GET /api/employee-overtimes/stats).</summary>
public class EmployeeOvertimeStatsDto
{
    public decimal TotalOvertimeHours { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
}

public class EmployeeOvertimeListDto
{
    public int Id { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public DateOnly OvertimeDate { get; set; }
    public OvertimeType OvertimeType { get; set; }
    public string OvertimeTypeDescription { get; set; } = string.Empty;
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? HolidayName { get; set; }
    public string? RateRuleNameApplied { get; set; }
    public string? EmployeeComment { get; set; }
    public decimal DurationInHours { get; set; }
    public decimal RateMultiplierApplied { get; set; }
    public OvertimeStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public bool IsProcessedInPayroll { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Helper static pour décrire les types d'overtime (flags combinés)</summary>
public static class OvertimeTypeHelper
{
    public static string GetDescription(OvertimeType type)
    {
        if (type == OvertimeType.None)
            return "Aucun";
        var parts = new List<string>();
        if ((type & OvertimeType.PublicHoliday) != 0)
            parts.Add("Jour férié");
        else if ((type & OvertimeType.WeeklyRest) != 0)
            parts.Add("Repos hebdomadaire");
        else if ((type & OvertimeType.Standard) != 0)
            parts.Add("Standard");
        if ((type & OvertimeType.Night) != 0)
            parts.Add("Nuit");
        return parts.Count > 0 ? string.Join(" + ", parts) : type.ToString();
    }

    public static string GetDescriptionEn(OvertimeType type)
    {
        if (type == OvertimeType.None)
            return "None";
        var parts = new List<string>();
        if ((type & OvertimeType.PublicHoliday) != 0)
            parts.Add("Public Holiday");
        else if ((type & OvertimeType.WeeklyRest) != 0)
            parts.Add("Weekly Rest");
        else if ((type & OvertimeType.Standard) != 0)
            parts.Add("Standard");
        if ((type & OvertimeType.Night) != 0)
            parts.Add("Night");
        return parts.Count > 0 ? string.Join(" + ", parts) : type.ToString();
    }
}

// ════════════════════════════════════════════════════════════
// ATTENDANCE
// ════════════════════════════════════════════════════════════

public class EmployeeAttendanceCreateDto
{
    public int EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public AttendanceSource Source { get; set; }
}

public class EmployeeAttendanceReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateOnly WorkDate { get; set; }
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public decimal WorkedHours { get; set; }
    public int BreakMinutesApplied { get; set; }
    public AttendanceStatus Status { get; set; }
    public AttendanceSource Source { get; set; }
    public List<EmployeeAttendanceBreakReadDto> Breaks { get; set; } = new();
}

/// <summary>DTO pour PATCH pointage (champs optionnels).</summary>
public class EmployeeAttendanceUpdateDto
{
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
}

public class EmployeeAttendanceCheckDto
{
    public int EmployeeId { get; set; }
}

// ── Breaks ────────────────────────────────────────────────

public class EmployeeAttendanceBreakCreateDto
{
    [Required]
    public int AttendanceId { get; set; }

    [Required]
    public TimeOnly BreakStart { get; set; }

    [Required]
    public TimeOnly BreakEnd { get; set; }

    [MaxLength(50)]
    public string? BreakType { get; set; }
}

/// <summary>PUT pause (même contrat que l'ancien backend, sans AttendanceId).</summary>
public class EmployeeAttendanceBreakUpdateDto
{
    [Required]
    public TimeOnly BreakStart { get; set; }

    [Required]
    public TimeOnly BreakEnd { get; set; }

    [MaxLength(50)]
    public string? BreakType { get; set; }
}

public class EmployeeAttendanceBreakReadDto
{
    public int Id { get; set; }
    public TimeOnly BreakStart { get; set; }

    /// <summary>Null = break still open</summary>
    public TimeOnly? BreakEnd { get; set; }
    public string BreakType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}

public class StartBreakDto
{
    [Required]
    public int AttendanceId { get; set; }

    [Required]
    public TimeOnly BreakStart { get; set; }

    [MaxLength(50)]
    public string? BreakType { get; set; }
}

public class EndBreakDto
{
    [Required]
    public TimeOnly BreakEnd { get; set; }
}

public class EmployeeBreakDto
{
    public int Id { get; set; }
    public DateTime BreakStart { get; set; }
    public DateTime? BreakEnd { get; set; }
    public string BreakType { get; set; } = string.Empty;
}

public class EmployeeDailyBreaksDto
{
    public DateTime Date { get; set; }
    public int TotalBreakMinutes { get; set; }
    public int BreakCount { get; set; }
    public List<EmployeeBreakDto> Breaks { get; set; } = new();
}

// ════════════════════════════════════════════════════════════
// CATEGORY
// ════════════════════════════════════════════════════════════

public class EmployeeCategoryCreateDto
{
    [Required(ErrorMessage = "L'ID de la société est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Le nom de la catégorie est requis")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caractères")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Le mode de la catégorie est requis")]
    public EmployeeCategoryMode Mode { get; set; }

    [StringLength(50, ErrorMessage = "La périodicité de paie ne peut pas dépasser 50 caractères")]
    public string? PayrollPeriodicity { get; set; }
}

public class EmployeeCategoryUpdateDto
{
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caractères")]
    public string? Name { get; set; }

    public EmployeeCategoryMode? Mode { get; set; }

    [StringLength(50, ErrorMessage = "La périodicité de paie ne peut pas dépasser 50 caractères")]
    public string? PayrollPeriodicity { get; set; }
}

public class EmployeeCategoryReadDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EmployeeCategoryMode Mode { get; set; }
    public string PayrollPeriodicity { get; set; } = "Mensuelle";
    public string ModeDescription { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// SAGE IMPORT
// ════════════════════════════════════════════════════════════

/// <summary>Représente une ligne du fichier CSV exporté depuis Sage Paie</summary>
public class SageImportRowDto
{
    public string? Matricule { get; set; }
    public string? Prenom { get; set; }
    public string? Nom { get; set; }
    public string? CIN { get; set; }
    public string? DateNaissance { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? CNSS { get; set; }
    public string? Salaire { get; set; }
    public string? DateEntree { get; set; }
    public string? Genre { get; set; }
    public string? Adresse { get; set; }
    public string? SituationFamiliale { get; set; }
    public string? EmploiOccupe { get; set; }
    public string? TauxAnc { get; set; }
    public string? Anct { get; set; }
    public string? TauxHoraire { get; set; }
}

public class SageImportResultDto
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<SageImportCreatedItemDto> Created { get; set; } = new();
    public List<SageImportUpdatedItemDto> Updated { get; set; } = new();
    public List<SageImportErrorDto> Errors { get; set; } = new();
}

public class SageImportCreatedItemDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? Matricule { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class SageImportUpdatedItemDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? Matricule { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class SageImportErrorDto
{
    public int Row { get; set; }
    public string? FullName { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ════════════════════════════════════════════════════════════
// TIMESHEET IMPORT
// ════════════════════════════════════════════════════════════

public class TimesheetImportResultDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string PeriodMode { get; set; } = "monthly";
    public int? Half { get; set; }
    public int TotalLines { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<TimesheetImportErrorDto> Errors { get; set; } = new();
}

public class TimesheetImportErrorDto
{
    public int Row { get; set; }
    public string? Matricule { get; set; }
    public string Message { get; set; } = string.Empty;
}
