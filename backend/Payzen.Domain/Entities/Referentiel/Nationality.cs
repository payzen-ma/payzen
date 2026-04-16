using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Referentiel;

public class Nationality : BaseEntity
{
    public required string Name { get; set; }

    // Navigation properties
    public ICollection<Employee.Employee>? Employees { get; set; }
}
