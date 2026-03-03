using payzen_backend.Models.Referentiel;

namespace payzen_backend.Models.Company
{
    /// <summary>
    /// Gestion des jours fériés par entreprise et pays
    /// </summary>
    public class Holiday
    {
        public int Id { get; set; }

        // Multilingue
        public required string NameFr { get; set; }
        public required string NameAr { get; set; }
        public required string NameEn { get; set; }

        public DateOnly HolidayDate { get; set; }
        public string? Description { get; set; }

        // Niveaux de gestion
        public int? CompanyId { get; set; } // NULL = Holiday global
        public int CountryId { get; set; }
        
        // Catégorisation
        public HolidayScope Scope { get; set; } // Global, Company
        public string HolidayType { get; set; } // National, Religieux, Company...
        public bool IsMandatory { get; set; } = true; // Légal vs optionnel
        public bool IsPaid { get; set; } = true;

        // Récurrence
        public bool IsRecurring { get; set; } = false;
        public string? RecurrenceRule { get; set; }
        public int? Year { get; set; }

        // Impact systeme
        public bool AffectPayroll { get; set; } = true;
        public bool AffectAttendance { get; set; } = true;

        // Activation par entreprise
        public bool IsActive { get; set; } = true;

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ModifiedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Company? Company { get; set; }
        public Country? Country { get; set; }
    }

    public enum HolidayScope
    {
        Global = 0,   // Au niveau pays (tous les companies du pays)
        Company = 1   // Spécifique à une entreprise
    }
}
