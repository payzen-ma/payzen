using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Events;

public class EmployeeEventLog : BaseEntity
{
    public int employeeId { get; set; }
    public string eventName { get; set; } = null!;
    public string? oldValue { get; set; }
    public int? oldValueId { get; set; }
    public string? newValue { get; set; }
    public int? newValueId { get; set; }
}
