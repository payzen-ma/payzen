using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Payroll.Dtos
{
    public class SalaryPackageItemWriteDto
    {
        public int? Id { get; set; }

        /// <summary>
        /// Reference to global component catalog (optional)
        /// </summary>
        public int? PayComponentId { get; set; }

        /// <summary>
        /// Reference to referentiel element for rule-driven payroll (optional).
        /// When set, CNSS/IR/CIMR treatment is determined by ElementRules instead of IsTaxable/IsSocial/IsCIMR.
        /// </summary>
        public int? ReferentielElementId { get; set; }

        [Required(ErrorMessage = "Item label is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Item label must be between 1 and 200 characters")]
        public string Label { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Default value must be positive")]
        public decimal DefaultValue { get; set; }

        public int? SortOrder { get; set; }

        // Moroccan regulatory fields (2026 compliance)

        /// <summary>
        /// Component type: base_salary, allowance, bonus, benefit_in_kind, social_charge
        /// </summary>
        [Required(ErrorMessage = "Item type is required")]
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
        /// Monthly estimate vs fixed amount
        /// </summary>
        public bool IsVariable { get; set; } = false;

        /// <summary>
        /// Cap for tax/social exemptions in MAD (e.g., Transport: 500/750 MAD)
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Exemption limit must be positive")]
        public decimal? ExemptionLimit { get; set; }
    }
}
