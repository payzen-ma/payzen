using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Models.Leave.Dtos
{
    public class LeaveRequestExemptionCreateDto
    {
        [Required] public int LeaveRequestId { get; set; }
        [Required] public DateOnly ExemptionDate { get; set; }

        [Required] public LeaveExemptionReasonType ReasonType { get; set; }

        // Par défaut : la date est exclue du congé (CountsAsLeaveDay=false)
        public bool CountsAsLeaveDay { get; set; } = false;

        public int? HolidayId { get; set; }
        public int? EmployeeAbsenceId { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }

    public class LeaveRequestExemptionPatchDto
    {
        public DateOnly? ExemptionDate { get; set; }
        public LeaveExemptionReasonType? ReasonType { get; set; }
        public bool? CountsAsLeaveDay { get; set; }
        public int? HolidayId { get; set; }
        public int? EmployeeAbsenceId { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }

    public class LeaveRequestExemptionReadDto
    {
        public int Id { get; set; }
        public int LeaveRequestId { get; set; }
        public DateOnly ExemptionDate { get; set; }
        public LeaveExemptionReasonType ReasonType { get; set; }
        public bool CountsAsLeaveDay { get; set; }

        public int? HolidayId { get; set; }
        public int? EmployeeAbsenceId { get; set; }
        public string? Note { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }
}
