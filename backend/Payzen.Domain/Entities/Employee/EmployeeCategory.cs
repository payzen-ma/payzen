using Payzen.Domain.Common;
using Payzen.Domain.Enums;

namespace Payzen.Domain.Entities.Employee;

public class EmployeeCategory : BaseEntity
{
    public int CompanyId { get; set; }
    public Company.Company Company { get; set; } = null!;
    public string Name { get; set; } = null!;
    public EmployeeCategoryMode Mode { get; set; }
    public string PayrollPeriodicity { get; set; } = "Mensuelle";


    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
