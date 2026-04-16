using Payzen.Domain.Common;
using Payzen.Domain.Enums;

namespace Payzen.Domain.Entities.Employee;

public class EmployeeAbsence : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateOnly AbsenceDate { get; set; }
    public AbsenceDurationType DurationType { get; set; }
    public AbsenceStatus Status { get; set; }
    public DateTimeOffset? DecisionAt { get; set; }
    public int? DecisionBy { get; set; }
    public string? DecisionComment { get; set; }

    // Demi-journée
    public bool? IsMorning { get; set; } // true = matin, false = après-midi

    // Tranche horaire
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public string AbsenceType { get; set; } = null!;
    public string? Reason { get; set; }
}
