namespace Payzen.Application.DTOs.Payroll;

public class CnssPreetabliParseResultDto
{
    public int? ImportId { get; set; }
    public DateTimeOffset? ImportedAt { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public CnssPreetabliHeaderDto? Header { get; set; }
    public List<CnssPreetabliEmployeeRowDto> Employees { get; set; } = [];
    public CnssPreetabliSummaryDto? Summary { get; set; }
    public List<CnssPreetabliIssueDto> Issues { get; set; } = [];
}

public class CnssPreetabliHeaderDto
{
    public string NatureRecordType { get; set; } = "A00";
    public string TransferIdentifier { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ReservedZoneA00 { get; set; } = string.Empty;
    public string GlobalHeaderRecordType { get; set; } = "A01";
    public string AffiliateNumber { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string AgencyCode { get; set; } = string.Empty;
    public string EmissionDateRaw { get; set; } = string.Empty;
    public string ExigibilityDateRaw { get; set; } = string.Empty;
}

public class CnssPreetabliEmployeeRowDto
{
    public int LineNumber { get; set; }
    public string RecordType { get; set; } = "A02";
    public string AffiliateNumber { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string InsuredNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int ChildrenCount { get; set; }
    public int FamilyAllowanceToPayCentimes { get; set; }
    public int FamilyAllowanceToDeductCentimes { get; set; }
    public int FamilyAllowanceNetToPayCentimes { get; set; }
    public decimal FamilyAllowanceToPay { get; set; }
    public decimal FamilyAllowanceToDeduct { get; set; }
    public decimal FamilyAllowanceNetToPay { get; set; }
    public string ReservedZone { get; set; } = string.Empty;
}

public class CnssPreetabliSummaryDto
{
    public string RecordType { get; set; } = "A03";
    public string AffiliateNumber { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int TotalChildren { get; set; }
    public decimal TotalFamilyAllowanceToPay { get; set; }
    public decimal TotalFamilyAllowanceToDeduct { get; set; }
    public decimal TotalFamilyAllowanceNetToPay { get; set; }
    public long TotalInsuredNumbers { get; set; }
    public string ReservedZone { get; set; } = string.Empty;
}

public class CnssPreetabliIssueDto
{
    public int LineNumber { get; set; }
    public string Severity { get; set; } = "error";
    public string Message { get; set; } = string.Empty;
}
