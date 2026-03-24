namespace payzen_backend.DTOs.Dashboard
{
    public class EmployeeDashboardDataDto
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public string Matricule { get; set; } = string.Empty;
        public string Manager { get; set; } = string.Empty;
        public string Seniority { get; set; } = string.Empty;

        public decimal SalaryNet { get; set; }
        public string PaidDate { get; set; } = string.Empty;

        public int LeavesRemaining { get; set; }
        public int LeavesTotal { get; set; }

        public int PresenceDays { get; set; }
        public int PresenceTotal { get; set; }

        public decimal ExtraHours { get; set; }

        public List<LeaveDetailDto> LeavesDetails { get; set; } = new();
        public List<ContractInfoDto> ContractInfo { get; set; } = new();
        public List<PayslipDetailDto> PayslipDetails { get; set; } = new();
        public List<EmployeeDocumentDto> Documents { get; set; } = new();
    }

    public class LeaveDetailDto
    {
        public string Label { get; set; } = string.Empty;
        public int? Remaining { get; set; }
        public int? Total { get; set; }
        public string ColorClass { get; set; } = string.Empty;
        public bool? IsText { get; set; }
        public string? Text { get; set; }
    }

    public class ContractInfoDto
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool? IsTag { get; set; }
        public string? TagColor { get; set; }
    }

    public class PayslipDetailDto
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class EmployeeDocumentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
