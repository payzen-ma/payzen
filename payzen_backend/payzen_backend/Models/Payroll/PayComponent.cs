namespace payzen_backend.Models.Payroll
{
    /// <summary>
    /// Global pay component catalog for Moroccan payroll compliance.
    /// Components define regulatory rules (tax, CNSS, CIMR, exemptions) that can be reused across packages.
    /// </summary>
    public class PayComponent
    {
        public int Id { get; set; }

        /// <summary>
        /// Unique code for the component (e.g., "BASE_SALARY", "TRANSPORT", "PRIME_ANCIENNETE")
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// French name of the component
        /// </summary>
        public required string NameFr { get; set; }

        /// <summary>
        /// Arabic name of the component
        /// </summary>
        public string? NameAr { get; set; }

        /// <summary>
        /// English name of the component
        /// </summary>
        public string? NameEn { get; set; }

        /// <summary>
        /// Component type: base_salary, allowance, bonus, benefit_in_kind, social_charge
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Subject to IR (Impot sur le Revenu) - Income Tax
        /// </summary>
        public bool IsTaxable { get; set; } = true;

        /// <summary>
        /// Subject to CNSS contributions (Caisse Nationale de Sécurité Sociale)
        /// </summary>
        public bool IsSocial { get; set; } = true;

        /// <summary>
        /// Subject to CIMR contributions (Caisse Interprofessionnelle Marocaine de Retraite)
        /// </summary>
        public bool IsCIMR { get; set; } = false;

        /// <summary>
        /// Cap for tax/social exemptions in MAD (e.g., Transport allowance: 500/750 MAD)
        /// Based on arrêté n°1314-25 (Oct 1, 2025) and subsequent regulations
        /// </summary>
        public decimal? ExemptionLimit { get; set; }

        /// <summary>
        /// Reference to legal basis for exemption rules (e.g., "arrete_1314_25", "code_travail_art_57")
        /// </summary>
        public string? ExemptionRule { get; set; }

        /// <summary>
        /// Default amount for this component (can be overridden in package items)
        /// </summary>
        public decimal? DefaultAmount { get; set; }

        /// <summary>
        /// Component version number for tracking regulatory changes
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Effective start date for this component version
        /// </summary>
        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Effective end date for this component version (null if currently active)
        /// </summary>
        public DateTime? ValidTo { get; set; }

        /// <summary>
        /// True if this is a regulated component (locked logic, backoffice only)
        /// False if custom component (tenant can modify within guardrails)
        /// </summary>
        public bool IsRegulated { get; set; } = false;

        /// <summary>
        /// True if this component is currently active and can be used in packages
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Display order in component lists
        /// </summary>
        public int SortOrder { get; set; } = 0;

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public ICollection<SalaryPackageItem>? PackageItems { get; set; }
    }
}
