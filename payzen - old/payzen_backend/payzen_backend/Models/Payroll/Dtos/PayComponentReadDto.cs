namespace payzen_backend.Models.Payroll.Dtos
{
    /// <summary>
    /// DTO for reading pay component catalog entries
    /// </summary>
    public class PayComponentReadDto
    {
        public int Id { get; set; }

        /// <summary>
        /// Unique code for the component (e.g., "BASE_SALARY", "TRANSPORT")
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// French name of the component
        /// </summary>
        public string NameFr { get; set; } = string.Empty;

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
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Subject to IR (Impot sur le Revenu) - Income Tax
        /// </summary>
        public bool IsTaxable { get; set; }

        /// <summary>
        /// Subject to CNSS contributions
        /// </summary>
        public bool IsSocial { get; set; }

        /// <summary>
        /// Subject to CIMR contributions
        /// </summary>
        public bool IsCIMR { get; set; }

        /// <summary>
        /// Cap for tax/social exemptions in MAD
        /// </summary>
        public decimal? ExemptionLimit { get; set; }

        /// <summary>
        /// Reference to legal basis for exemption rules
        /// </summary>
        public string? ExemptionRule { get; set; }

        /// <summary>
        /// Default amount for this component
        /// </summary>
        public decimal? DefaultAmount { get; set; }

        /// <summary>
        /// Component version number
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Effective start date for this component version
        /// </summary>
        public DateTime ValidFrom { get; set; }

        /// <summary>
        /// Effective end date for this component version
        /// </summary>
        public DateTime? ValidTo { get; set; }

        /// <summary>
        /// True if this is a regulated component (locked logic)
        /// </summary>
        public bool IsRegulated { get; set; }

        /// <summary>
        /// True if this component is currently active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Display order in component lists
        /// </summary>
        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
