namespace payzen_backend.Models.Payroll.Dtos
{
    public class SalaryPackageReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BaseSalary { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public int BusinessSectorId { get; set; }
        public string? BusinessSectorName { get; set; }

        // Template type distinction (OFFICIAL for backoffice, COMPANY for client-owned)
        public string TemplateType { get; set; } = "OFFICIAL";

        // Moroccan regulation version
        public string RegulationVersion { get; set; } = "MA_2025";

        // Auto rules configuration (Morocco 2025)
        public AutoRulesDto? AutoRules { get; set; }

        // CIMR configuration (Morocco 2025)
        public CimrConfigDto? CimrConfig { get; set; }

        // Origin tracking for copied templates
        public string? OriginType { get; set; }
        public string? SourceTemplateNameSnapshot { get; set; }
        public DateTime? CopiedAt { get; set; }

        // CIMR and insurance (Moroccan 2025 compliance) - Legacy fields
        public decimal? CimrRate { get; set; }
        public bool HasPrivateInsurance { get; set; }

        // Versioning and template tracking
        public int Version { get; set; }
        public int? SourceTemplateId { get; set; }
        public string? SourceTemplateName { get; set; }
        public int? SourceTemplateVersion { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public bool IsLocked { get; set; }

        /// <summary>
        /// True if this is a global template (CompanyId is null)
        /// </summary>
        public bool IsGlobalTemplate => CompanyId == null;

        public List<SalaryPackageItemReadDto> Items { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
