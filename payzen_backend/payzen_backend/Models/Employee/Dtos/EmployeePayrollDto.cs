namespace payzen_backend.Models.Employee.Dtos;

public class EmployeePayrollDto
{
    // Infos personnelles
    public string FullName { get; set; }
    public string CinNumber { get; set; }
    public string CnssNumber { get; set; }
    public string CimrNumber { get; set; }
    public decimal? CimrEmployeeRate { get; set; }
    public decimal? CimrCompanyRate { get; set; }
    public bool HasPrivateInsurance { get; set; }
    public decimal? PrivateInsuranceRate { get; set; }
    public bool DisableAmo { get; set; }
    public string MaritalStatus { get; set; }
    public int NumberOfChildren { get; set; }
    public bool HasSpouse { get; set; }

    // Contrat actif
    public string ContractType { get; set; }
    public string LegalContractType { get; set; }
    public string StateEmploymentProgram { get; set; }
    public string JobPosition { get; set; }
    public DateTime ContractStartDate { get; set; }
    public int AncienneteYears { get; set; }

    // Salaire
    public decimal BaseSalary { get; set; }
    public decimal? BaseSalaryHourly { get; set; }
    public List<PayrollSalaryComponentDto> SalaryComponents { get; set; }

    // Package salarial
    public string SalaryPackageName { get; set; }
    public List<PayrollPackageItemDto> PackageItems { get; set; }

    // Absences du mois
    public List<PayrollAbsenceDto> Absences { get; set; }

    // Heures supplémentaires du mois
    public List<PayrollOvertimeDto> Overtimes { get; set; }

    // Congés pris ce mois
    public List<PayrollLeaveDto> Leaves { get; set; }

    // Période de paie
    public int PayMonth { get; set; }
    public int PayYear { get; set; }

    // Heures travaillées importées (pointage)
    public decimal TotalWorkedHours { get; set; }
}

public class PayrollSalaryComponentDto
{
    public string ComponentType { get; set; }
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsSocial { get; set; }
    public bool IsCIMR { get; set; }
}

public class PayrollPackageItemDto
{
    public string Label { get; set; }
    public string Type { get; set; }
    public decimal DefaultValue { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsSocial { get; set; }
    public bool IsCIMR { get; set; }
    public decimal? ExemptionLimit { get; set; }
}

public class PayrollAbsenceDto
{
    public string AbsenceType { get; set; }
    public DateTime AbsenceDate { get; set; }
    public string DurationType { get; set; }
    public string Status { get; set; }
}

public class PayrollOvertimeDto
{
    public DateTime OvertimeDate { get; set; }
    public decimal DurationInHours { get; set; }
    public decimal RateMultiplier { get; set; }
}

public class PayrollLeaveDto
{
    public string LeaveType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal DaysCount { get; set; }
}
