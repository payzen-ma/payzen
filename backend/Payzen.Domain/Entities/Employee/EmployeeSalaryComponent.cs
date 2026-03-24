using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Employee;

public class EmployeeSalaryComponent : BaseEntity
{
    public int EmployeeSalaryId { get; set; }
    public required string ComponentType { get; set; } // Prime, Indemnité, Déduction, Bonus, etc.
    public required bool IsTaxable { get; set; }
    public required bool IsSocial { get; set; }
    public required bool IsCIMR { get; set; }
    public decimal Amount { get; set; }
    public required DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Navigation properties
    public EmployeeSalary? EmployeeSalary { get; set; } = null!;
}
