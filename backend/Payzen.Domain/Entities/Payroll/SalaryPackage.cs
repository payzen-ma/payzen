using Payzen.Domain.Common;
using Payzen.Domain.Entities.Payroll.Referentiel;

namespace Payzen.Domain.Entities.Payroll;

public class SalaryPackage : BaseEntity
{
    public required string Name { get; set; }
    public required string Category { get; set; }
    public string? Description { get; set; }
    public decimal BaseSalary { get; set; }
    public required string Status { get; set; } // draft, published, deprecated
    public int? CompanyId { get; set; }

    public int BusinessSectorId { get; set; }
    public BusinessSector? BusinessSector { get; set; }

    public string TemplateType { get; set; } = "OFFICIAL";
    public string RegulationVersion { get; set; } = "MA_2025";

    public string? AutoRulesJson { get; set; }
    public string? CimrConfigJson { get; set; }

    public string? OriginType { get; set; }
    public string? SourceTemplateNameSnapshot { get; set; }
    public DateTime? CopiedAt { get; set; }

    // Legacy CIMR fields
    public decimal? CimrRate { get; set; }
    public bool HasPrivateInsurance { get; set; } = false;

    // Versioning
    public int Version { get; set; } = 1;
    public int? SourceTemplateId { get; set; }
    public int? SourceTemplateVersion { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsLocked { get; set; } = false;


    // Navigation
    public Company.Company? Company { get; set; }
    public ICollection<SalaryPackageItem> Items { get; set; } = new List<SalaryPackageItem>();
    public ICollection<SalaryPackageAssignment> Assignments { get; set; } = new List<SalaryPackageAssignment>();
}
