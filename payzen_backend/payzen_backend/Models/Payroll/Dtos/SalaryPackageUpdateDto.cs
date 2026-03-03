using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Payroll.Dtos
{
    public class SalaryPackageUpdateDto
    {
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
        public string? Name { get; set; }

        [StringLength(100, MinimumLength = 2, ErrorMessage = "Category must be between 2 and 100 characters")]
        public string? Category { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Base salary must be positive")]
        public decimal? BaseSalary { get; set; }

        [RegularExpression("^(draft|published|deprecated)$", ErrorMessage = "Status must be one of: draft, published, deprecated")]
        public string? Status { get; set; }

        public int? CompanyId { get; set; }

        [Required(ErrorMessage = "Le secteur d'activité est requis")]
        public int BusinessSectorId { get; set; }

        // Template type distinction (OFFICIAL for backoffice, COMPANY for client-owned)
        [RegularExpression("^(OFFICIAL|COMPANY)$", ErrorMessage = "TemplateType must be OFFICIAL or COMPANY")]
        public string? TemplateType { get; set; }

        // Moroccan regulation version
        public string? RegulationVersion { get; set; }

        // Auto rules configuration (Morocco 2025)
        public AutoRulesDto? AutoRules { get; set; }

        // CIMR configuration (Morocco 2025)
        public CimrConfigDto? CimrConfig { get; set; }

        // CIMR and insurance (Moroccan 2025 compliance) - Legacy fields

        /// <summary>
        /// CIMR contribution rate (3% to 12%) - Legacy field, use CimrConfig instead
        /// </summary>
        [Range(0, 0.12, ErrorMessage = "CIMR rate must be between 0 and 12%")]
        public decimal? CimrRate { get; set; }

        /// <summary>
        /// Whether employees have private health insurance
        /// </summary>
        public bool? HasPrivateInsurance { get; set; }

        // Effective dates

        /// <summary>
        /// Effective start date for this package
        /// </summary>
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        /// Effective end date for this package
        /// </summary>
        public DateTime? ValidTo { get; set; }

        public List<SalaryPackageItemWriteDto>? Items { get; set; }
    }
}
