using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Employee;

public class EmployeeContract : BaseEntity
{
    public int EmployeeId { get; set; }
    public int CompanyId { get; set; }
    public int JobPositionId { get; set; }
    public int ContractTypeId { get; set; }
    public required DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ExonerationEndDate { get; set; }

    // Navigation properties
    public Employee? Employee { get; set; } = null!;
    public Company.Company? Company { get; set; } = null!;
    public Company.JobPosition? JobPosition { get; set; } = null!;
    public Company.ContractType? ContractType { get; set; } = null!;
    public ICollection<EmployeeSalary>? Salaries { get; set; }
}
