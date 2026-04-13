using Payzen.Domain.Common;
using Payzen.Domain.Entities.Referentiel;

namespace Payzen.Domain.Entities.Employee;

public class EmployeeSpouse : BaseEntity
{
    public int EmployeeId
    {
        get; set;
    }
    public required string FirstName
    {
        get; set;
    }
    public required string LastName
    {
        get; set;
    }
    public required DateTime DateOfBirth
    {
        get; set;
    }
    public int? GenderId
    {
        get; set;
    }
    public string? CinNumber
    {
        get; set;
    }
    public bool IsDependent { get; set; } = false;

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Gender? Gender
    {
        get; set;
    }
}
