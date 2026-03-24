using Payzen.Domain.Entities.Referentiel;
using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Employee;

public class EmployeeAddress : BaseEntity
{
    public int EmployeeId { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string ZipCode { get; set; }
    public int CityId { get; set; }

    // Navigation properties
    public Employee? Employee { get; set; } = null!;
    public City? City { get; set; } = null!;
}
