using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Company;

public class JobPosition : BaseEntity
{
    public required string Name { get; set; }
    public int CompanyId { get; set; }

    // Navigation properties
    public Company? Company { get; set; } = null!;
    public ICollection<Employee.EmployeeContract>? EmployeeContracts { get; set; }
}
