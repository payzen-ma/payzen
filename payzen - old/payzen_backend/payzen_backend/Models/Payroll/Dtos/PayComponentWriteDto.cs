using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Payroll.Dtos
{
    /// <summary>
    /// DTO for creating/updating pay component catalog entries
    /// </summary>
    public class PayComponentWriteDto
    {
        /// <summary>
        /// Unique code for the component (e.g., "BASE_SALARY", "TRANSPORT")
        /// </summary>
        [Required(ErrorMessage = "Code is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 50 characters")]
        [RegularExpression("^[A-Z0-9_]+$", ErrorMessage = "Code must contain only uppercase letters, numbers, and underscores")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// French name of the component
        /// </summary>
        [Required(ErrorMessage = "French name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "French name must be between 2 and 200 characters")]
        public string NameFr { get; set; } = string.Empty;

        /// <summary>
        /// Arabic name of the component
        /// </summary>
        [StringLength(200, ErrorMessage = "Arabic name cannot exceed 200 characters")]
        public string? NameAr { get; set; }

        /// <summary>
        /// English name of the component
        /// </summary>
        [StringLength(200, ErrorMessage = "English name cannot exceed 200 characters")]
        public string? NameEn { get; set; }

        /// <summary>
        /// Component type: base_salary, allowance, bonus, benefit_in_kind, social_charge
        /// </summary>
        [Required(ErrorMessage = "Type is required")]
        [RegularExpression("^(base_salary|allowance|bonus|benefit_in_kind|social_charge)$", 
            ErrorMessage = "Type must be one of: base_salary, allowance, bonus, benefit_in_kind, social_charge")]
        public string Type { get; set; } = "allowance";

        /// <summary>
        /// Subject to IR (Impot sur le Revenu) - Income Tax
        /// </summary>
        public bool IsTaxable { get; set; } = true;

        /// <summary>
        /// Subject to CNSS contributions
        /// </summary>
        public bool IsSocial { get; set; } = true;

        /// <summary>
        /// Subject to CIMR contributions
        /// </summary>
        public bool IsCIMR { get; set; } = false;

        /// <summary>
        /// Cap for tax/social exemptions in MAD
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Exemption limit must be positive")]
        public decimal? ExemptionLimit { get; set; }

        /// <summary>
        /// Reference to legal basis for exemption rules (e.g., "arrete_1314_25")
        /// </summary>
        [StringLength(100, ErrorMessage = "Exemption rule cannot exceed 100 characters")]
        public string? ExemptionRule { get; set; }

        /// <summary>
        /// Default amount for this component
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Default amount must be positive")]
        public decimal? DefaultAmount { get; set; }

        /// <summary>
        /// Effective start date for this component version
        /// </summary>
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        /// Effective end date for this component version
        /// </summary>
        public DateTime? ValidTo { get; set; }

        /// <summary>
        /// True if this is a regulated component (locked logic, backoffice only)
        /// </summary>
        public bool IsRegulated { get; set; } = false;

        /// <summary>
        /// True if this component is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Display order in component lists
        /// </summary>
        public int? SortOrder { get; set; }
    }
}
