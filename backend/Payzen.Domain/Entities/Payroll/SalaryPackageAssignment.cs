using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll;

public class SalaryPackageAssignment : BaseEntity
{
    public int SalaryPackageId { get; set; }
    public int EmployeeId { get; set; }
    public int ContractId { get; set; }
    public int EmployeeSalaryId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PackageVersion { get; set; }

    // Navigation
    public SalaryPackage? SalaryPackage { get; set; }
    public Employee.Employee? Employee { get; set; }
    public Employee.EmployeeContract? Contract { get; set; }
    public Employee.EmployeeSalary? EmployeeSalary { get; set; }
}
