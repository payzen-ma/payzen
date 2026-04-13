using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Employee;

public class EmployeeDocument : BaseEntity
{
    public int EmployeeId
    {
        get; set;
    }
    public required string Name
    {
        get; set;
    }
    public required string FilePath
    {
        get; set;
    }
    public DateTime? ExpirationDate
    {
        get; set;
    }
    public required string DocumentType
    {
        get; set;
    }

    // Navigation properties
    public Employee? Employee { get; set; } = null!;
}
