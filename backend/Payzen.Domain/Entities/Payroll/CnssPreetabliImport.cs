using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll;

public class CnssPreetabliImport : BaseEntity
{
    public int CompanyId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string AffiliateNumber { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int IssueCount { get; set; }
    public string Status { get; set; } = "parsed";

    public Company.Company? Company { get; set; }
    public ICollection<CnssPreetabliLine> Lines { get; set; } = [];
}
