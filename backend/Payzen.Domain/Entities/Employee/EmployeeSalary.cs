using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Employee;

public class EmployeeSalary : BaseEntity
{
    public int EmployeeId
    {
        get; set;
    }
    public int ContractId
    {
        get; set;
    }
    public decimal? BaseSalary
    {
        get; set;
    }
    public decimal? BaseSalaryHourly
    {
        get; set;
    }
    public required DateTime EffectiveDate
    {
        get; set;
    }
    public DateTime? EndDate
    {
        get; set;
    }

    // Navigation properties
    public Employee? Employee { get; set; } = null!;
    public EmployeeContract? Contract { get; set; } = null!;
    public ICollection<EmployeeSalaryComponent>? Components
    {
        get; set;
    }
}
