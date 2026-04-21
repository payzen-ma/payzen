namespace Payzen.Application.DTOs.Payroll;

public class CnssPreetabliParseResultDto
{
    public CnssPreetabliHeaderDto? Header { get; set; }
    public List<CnssPreetabliEmployeeRowDto> Employees { get; set; } = [];
    public CnssPreetabliSummaryDto? Summary { get; set; }
    public List<CnssPreetabliIssueDto> Issues { get; set; } = [];
}

public class CnssPreetabliHeaderDto
{
    public string TransferIdentifier { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
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
    public string AffiliateNumber { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string InsuredNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int ChildrenCount { get; set; }
    public decimal FamilyAllowanceToPay { get; set; }
    public decimal FamilyAllowanceToDeduct { get; set; }
    public decimal FamilyAllowanceNetToPay { get; set; }
}

public class CnssPreetabliSummaryDto
{
    public string AffiliateNumber { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int TotalChildren { get; set; }
    public decimal TotalFamilyAllowanceToPay { get; set; }
    public decimal TotalFamilyAllowanceToDeduct { get; set; }
    public decimal TotalFamilyAllowanceNetToPay { get; set; }
    public long TotalInsuredNumbers { get; set; }
}

public class CnssPreetabliIssueDto
{
    public int LineNumber { get; set; }
    public string Severity { get; set; } = "error";
    public string Message { get; set; } = string.Empty;
}
