using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Company;

public class Departement : BaseEntity
{
    public required string DepartementName { get; set; }
    public int CompanyId { get; set; }

    // Navigation properties
    public Company? Company { get; set; } = null!;
    public ICollection<Employee.Employee>? Employees { get; set; }
}
