using System.ComponentModel.DataAnnotations;
using Payzen.Domain.Common;
using Payzen.Domain.Enums;

namespace Payzen.Domain.Entities.Leave;

public class LeaveRequestExemption : BaseEntity
{
    public int LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;

    public DateOnly ExemptionDate { get; set; }
    public LeaveExemptionReasonType ReasonType { get; set; }
    public bool CountsAsLeaveDay { get; set; } = false;

    public int? HolidayId { get; set; }
    public Company.Holiday? Holiday { get; set; }

    public int? EmployeeAbsenceId { get; set; }
    public Employee.EmployeeAbsence? EmployeeAbsence { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
