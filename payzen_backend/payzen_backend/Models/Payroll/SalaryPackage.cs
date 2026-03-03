using CompanyEntity = payzen_backend.Models.Company.Company;
using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.Models.Payroll
{
    public class SalaryPackage
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Category { get; set; }
        public string? Description { get; set; }
        public decimal BaseSalary { get; set; }
        public required string Status { get; set; } // draft, published, deprecated
        public int? CompanyId { get; set; }

        // Business sector (required)
        public int BusinessSectorId { get; set; }
        public BusinessSector? BusinessSector { get; set; }

        // Template type distinction (OFFICIAL for backoffice, COMPANY for client-owned)
        public string TemplateType { get; set; } = "OFFICIAL";

        // Moroccan regulation version
        public string RegulationVersion { get; set; } = "MA_2025";

        // Auto rules configuration (stored as JSON)
        // Contains: { seniorityBonusEnabled: bool, ruleVersion: string }
        public string? AutoRulesJson { get; set; }

        // CIMR configuration (stored as JSON)
        // Contains: { regime: string, employeeRate: decimal, employerRate: decimal, customEmployerRate?: decimal }
        public string? CimrConfigJson { get; set; }

        // Origin tracking for copied templates
        public string? OriginType { get; set; } // CUSTOM or COPIED_FROM_OFFICIAL
        public string? SourceTemplateNameSnapshot { get; set; } // Name of source template at copy time
        public DateTime? CopiedAt { get; set; } // When the template was copied

        // CIMR and insurance (Moroccan 2025 compliance) - Legacy fields
        public decimal? CimrRate { get; set; } // 3% to 12% - kept for backwards compatibility
        public bool HasPrivateInsurance { get; set; } = false;

        // Versioning and template tracking
        public int Version { get; set; } = 1; // Package version number
        public int? SourceTemplateId { get; set; } // Reference to cloned-from global template
        public int? SourceTemplateVersion { get; set; } // Version of template when cloned
        public DateTime? ValidFrom { get; set; } // Effective start date
        public DateTime? ValidTo { get; set; } // Effective end date
        public bool IsLocked { get; set; } = false; // True if used in payroll (immutable)

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public CompanyEntity? Company { get; set; }
        public SalaryPackage? SourceTemplate { get; set; }
        public ICollection<SalaryPackage>? ClonedPackages { get; set; }
        public ICollection<SalaryPackageItem>? Items { get; set; }
        public ICollection<SalaryPackageAssignment>? Assignments { get; set; }
    }
}
