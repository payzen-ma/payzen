using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll;

public class CnssPreetabliLine : BaseEntity
{
    public int CnssPreetabliImportId { get; set; }
    public int LineNumber { get; set; }
    public string AffiliateNumber { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string InsuredNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int ChildrenCount { get; set; }
    public decimal FamilyAllowanceToPay { get; set; }
    public decimal FamilyAllowanceToDeduct { get; set; }
    public decimal FamilyAllowanceNetToPay { get; set; }

    public CnssPreetabliImport? Import { get; set; }
}
