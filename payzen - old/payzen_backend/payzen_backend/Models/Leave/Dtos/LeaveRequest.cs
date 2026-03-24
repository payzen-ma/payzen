using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Models.Leave.Dtos
{
public class LeaveRequestCreateDto
    {
        [Required]
        public int LeaveTypeId { get; set; }
        public int LeaveTypePolicyId { get; set; }

        // obligatoire si le type nécessite un cas légal (ex: MARRIAGE)
        public int? LegalRuleId { get; set; }

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        [MaxLength(1000)]
        public string? EmployeeNote { get; set; }
    }

    public class LeaveRequestPatchDto
    {
        public int? LeaveTypeId { get; set; }
        public int? LeaveTypePolicyId { get; set; }
        public int? LegalRuleId { get; set; }

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        public bool? IsRenounced { get; set; }

        [MaxLength(1000)]
        public string? EmployeeNote { get; set; }

        [MaxLength(1000)]
        public string? ManagerNote { get; set; }
    }

    public class LeaveRequestReadDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int CompanyId { get; set; }

        public int LeaveTypeId { get; set; }
        public string LeaveTypeCode { get; set; } = string.Empty;
        public string LeaveTypeName { get; set; } = string.Empty;

        public int LeaveTypePolicyId { get; set; }
        public string LevaeTypePolicyCode { get; set; } = string.Empty;
        public string LeaveTypePolicyName { get; set; } = string.Empty;

        public int? LegalRuleId { get; set; }
        public string? LegalCaseCode { get; set; }
        public string? LegalCaseDescription { get; set; }
        public int? LegalDaysGranted { get; set; }
        public string? LegalArticle { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public LeaveRequestStatus Status { get; set; }

        public DateTimeOffset RequestedAt { get; set; }
        public DateTimeOffset? SubmittedAt { get; set; }
        public DateTimeOffset? DecisionAt { get; set; }
        public int? DecisionBy { get; set; }
        public string? DecisionComment { get; set; }

        public int CalendarDays { get; set; }
        public decimal WorkingDaysDeducted { get; set; }
        public bool HasMinConsecutiveBlock { get; set; }
        public string? ComputationVersion { get; set; }

        public bool IsRenounced { get; set; }
        public string? EmployeeNote { get; set; }
        public string? ManagerNote { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
    } 
}
