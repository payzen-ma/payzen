using Payzen.Domain.Common;
using Payzen.Domain.Enums;

namespace Payzen.Domain.Entities.Employee;

public class EmployeeAttendanceBreak : BaseEntity
{
    public int EmployeeAttendanceId
    {
        get; set;
    }
    public EmployeeAttendance? EmployeeAttendance
    {
        get; set;
    }
    public TimeOnly BreakStart
    {
        get; set;
    }
    public TimeOnly? BreakEnd
    {
        get; set;
    }
    public string? BreakType
    {
        get; set;
    }

}
