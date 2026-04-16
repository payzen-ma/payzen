using System.ComponentModel.DataAnnotations;
using Payzen.Domain.Common;
using Payzen.Domain.Entities.Referentiel;

namespace Payzen.Domain.Entities.Company;

public class Company : BaseEntity
{
    [Required(ErrorMessage = "Le nom de l'entreprise est requis")]
    [StringLength(500, MinimumLength = 2)]
    public required string CompanyName { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(500)]
    public required string Email { get; set; }

    [Required]
    [StringLength(20)]
    public required string PhoneNumber { get; set; }

    [StringLength(10)]
    public string? CountryPhoneCode { get; set; }

    [Required]
    [StringLength(1000)]
    public required string CompanyAddress { get; set; }

    [Required]
    public required int CityId { get; set; }

    [Required]
    public required int CountryId { get; set; }

    [StringLength(100)]
    public string? CnssNumber { get; set; }

    public bool IsCabinetExpert { get; set; } = false;

    [StringLength(100)]
    public string? IceNumber { get; set; }

    [StringLength(100)]
    public string? IfNumber { get; set; }

    [StringLength(100)]
    public string? RcNumber { get; set; }
    public string? PatenteNumber { get; set; }

    [StringLength(100)]
    public string? RibNumber { get; set; }

    [StringLength(50)]
    public string? LegalForm { get; set; }

    [StringLength(200)]
    public string? SignatoryName { get; set; }

    [StringLength(100)]
    public string? SignatoryTitle { get; set; }

    public DateTime? FoundingDate { get; set; }
    public string? WebsiteUrl { get; set; }

    [StringLength(10)]
    public string Currency { get; set; } = "MAD";

    [StringLength(50)]
    public string PayrollPeriodicity { get; set; } = "Mensuelle";

    [Range(1, 12)]
    public int FiscalYearStartMonth { get; set; } = 1;

    [StringLength(200)]
    public string? BusinessSector { get; set; }

    [StringLength(100)]
    public string? PaymentMethod { get; set; }

    public int? ManagedByCompanyId { get; set; }

    // Convention pour choisir la stratégie d'authentification.
    // "C" = Microsoft Entra External ID (Type C) pour les utilisateurs non-backoffice.
    // Par défaut: "JWT" (login backoffice via email/mot de passe).
    [StringLength(20)]
    public string? AuthType { get; set; } = "JWT";
    public bool isActive { get; set; } = true;

    // Navigation properties
    public Company? ManagedByCompany { get; set; }
    public City? City { get; set; }
    public Country? Country { get; set; }
    public ICollection<Company>? ManagedCompanies { get; set; }
    public ICollection<Employee.Employee>? Employees { get; set; }
    public ICollection<CompanyDocument>? Documents { get; set; }
}
