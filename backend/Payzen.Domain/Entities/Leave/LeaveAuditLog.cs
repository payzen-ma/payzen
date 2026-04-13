using Payzen.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Payzen.Domain.Entities.Leave;

public class LeaveAuditLog : BaseEntity
{

    public int CompanyId
    {
        get; set;
    }
    public Company.Company Company { get; set; } = null!;

    public int? EmployeeId
    {
        get; set;
    }
    public Employee.Employee? Employee
    {
        get; set;
    }

    public int? LeaveRequestId
    {
        get; set;
    }
    public LeaveRequest? LeaveRequest
    {
        get; set;
    }

    [Required, MaxLength(200)] public string EventName { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string? OldValue
    {
        get; set;
    }
    [MaxLength(2000)]
    public string? NewValue
    {
        get; set;
    }
}
