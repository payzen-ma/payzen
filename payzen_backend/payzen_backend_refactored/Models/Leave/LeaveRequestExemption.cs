using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.LeaveStatus;
using payzen_backend.Models.Company;

namespace payzen_backend.Models.Leave
{
    // Role jour par jour, uniquement pour les exceptions (holiday, sick...)
    public class LeaveRequestExemption
    {
        public int Id { get; set; }

        public int LeaveRequestId { get; set; }
        public LeaveRequest LeaveRequest { get; set; } = null!;

        public DateOnly ExemptionDate { get; set; }

        public LeaveExemptionReasonType ReasonType { get; set; }

        public bool CountsAsLeaveDay { get; set; } = false;

        // Optionnels selon ReasonType
        public int? HolidayId { get; set; }
        public Holiday? Holiday { get; set; }

        public int? EmployeeAbsenceId { get; set; }
        public Employee.EmployeeAbsence? EmployeeAbsence { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
        public int? CreatedBy { get; set; } // Users.Id
    }
}
