using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company
{
    public class Company
    {
        public int Id { get; set; }

        // ========== INFORMATIONS DE BASE (Obligatoires à la création) ==========
        
        [Required(ErrorMessage = "Le nom de l'entreprise est requis")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caractères")]
        public required string CompanyName { get; set; }

        [Required(ErrorMessage = "L'email professionnel est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(500, ErrorMessage = "L'email ne peut pas dépasser 500 caractères")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Le numéro de téléphone est requis")]
        [StringLength(20, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 20 caractères")]
        public required string PhoneNumber { get; set; }

        [StringLength(10, ErrorMessage = "L'indicatif ne peut pas dépasser 10 caractères")]
        public string? CountryPhoneCode { get; set; }

        [Required(ErrorMessage = "L'adresse est requise")]
        [StringLength(1000, ErrorMessage = "L'adresse ne peut pas dépasser 1000 caractères")]
        public required string CompanyAddress { get; set; }

        [Required(ErrorMessage = "La ville est requise")]
        public required int CityId { get; set; }

        [Required(ErrorMessage = "Le pays est requis")]
        public required int CountryId { get; set; }

        [Required(ErrorMessage = "Le numéro CNSS employeur est requis")]
        [StringLength(100, ErrorMessage = "Le numéro CNSS ne peut pas dépasser 100 caractères")]
        public required string CnssNumber { get; set; }

        [Required(ErrorMessage = "Le type d'entreprise est requis")]
        public bool IsCabinetExpert { get; set; } = false;

        // ========== INFORMATIONS LÉGALES & FISCALES (Optionnelles à la création) ==========

        [StringLength(100, ErrorMessage = "Le numéro ICE ne peut pas dépasser 100 caractères")]
        public string? IceNumber { get; set; }

        [StringLength(100, ErrorMessage = "Le numéro IF ne peut pas dépasser 100 caractères")]
        public string? IfNumber { get; set; }

        [StringLength(100, ErrorMessage = "Le numéro RC ne peut pas dépasser 100 caractères")]
        public string? RcNumber { get; set; }
        public string? PatenteNumber { get; set; }

        [StringLength(100, ErrorMessage = "Le numéro RIB ne peut pas dépasser 100 caractères")]
        public string? RibNumber { get; set; }

        [StringLength(50, ErrorMessage = "La forme juridique ne peut pas dépasser 50 caractères")]
        public string? LegalForm { get; set; }

        // ========== SIGNATAIRE ==========

        [StringLength(200, ErrorMessage = "Le nom du signataire ne peut pas dépasser 200 caractères")]
        public string? SignatoryName { get; set; }

        [StringLength(100, ErrorMessage = "Le titre du signataire ne peut pas dépasser 100 caractères")]
        public string? SignatoryTitle { get; set; }

        public DateTime? FoundingDate { get; set; }
        public string? WebsiteUrl { get; set; }

        // ========== PARAMÉTRAGE PAIE (Optionnels avant 1ère paie) ==========

        [StringLength(10, ErrorMessage = "La devise ne peut pas dépasser 10 caractères")]
        public string Currency { get; set; } = "MAD";

        [StringLength(50, ErrorMessage = "La périodicité ne peut pas dépasser 50 caractères")]
        public string PayrollPeriodicity { get; set; } = "Mensuelle";

        [Range(1, 12, ErrorMessage = "Le mois de début d'exercice fiscal doit être entre 1 et 12")]
        public int FiscalYearStartMonth { get; set; } = 1;

        [StringLength(200, ErrorMessage = "Le secteur d'activité ne peut pas dépasser 200 caractères")]
        public string? BusinessSector { get; set; }

        [StringLength(100, ErrorMessage = "Le mode de paiement ne peut pas dépasser 100 caractères")]
        public string? PaymentMethod { get; set; }

        // ========== GESTION MULTI-ENTREPRISES (Cabinet comptable) ==========

        public int? ManagedByCompanyId { get; set; } // Cabinet comptable qui gère cette entreprise

        // ===================== Active Company or Disable it =================
        public bool isActive { get; set; } = true;
        
        // ========== CHAMPS D'AUDIT ==========

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // ========== NAVIGATION PROPERTIES ==========

        public Company? ManagedByCompany { get; set; }
        public ICollection<Company>? ManagedCompanies { get; set; }
        public ICollection<Employee.Employee>? Employees { get; set; }
        public Referentiel.City? City { get; set; }
        public Referentiel.Country? Country { get; set; }

        // Documents liés à l'entreprise (logo, statuts, ...)
        public ICollection<CompanyDocument>? Documents { get; set; }
    }
}
