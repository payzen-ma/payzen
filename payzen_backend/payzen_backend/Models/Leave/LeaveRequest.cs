using payzen_backend.Models.Common.LeaveStatus;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace payzen_backend.Models.Leave
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee.Employee Employee { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company.Company Company { get; set; } = null!;

        public int LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; } = null!;

        // Cas légal choisi (Mariage salarié vs enfant etc.)
        public int? LegalRuleId { get; set; }
        public LeaveTypeLegalRule? LegalRule { get; set; }

        // Snapshot de policy utilisée lors du calcul
        public int? PolicyId { get; set; }
        public LeaveTypePolicy? Policy { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Draft;

        public DateTimeOffset RequestedAt { get; set; }
        public DateTimeOffset? SubmittedAt { get; set; }
        public DateTimeOffset? DecisionAt { get; set; }
        public int? DecisionBy { get; set; } // Users.Id
        [MaxLength(1000)]
        public string? DecisionComment { get; set; }

        public int CalendarDays { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal WorkingDaysDeducted { get; set; } = 0m;

        public bool HasMinConsecutiveBlock { get; set; } = false;

        [MaxLength(50)]
        public string? ComputationVersion { get; set; }

        public bool IsRenounced { get; set; } = false;

        [MaxLength(1000)]
        public string? EmployeeNote { get; set; }
        [MaxLength(1000)]
        public string? ManagerNote { get; set; }

        public ICollection<LeaveRequestExemption> Exemptions { get; set; } = new List<LeaveRequestExemption>();
        public ICollection<LeaveRequestApprovalHistory> ApprovalHistory { get; set; } = new List<LeaveRequestApprovalHistory>();
        public ICollection<LeaveRequestAttachment> Attachments { get; set; } = new List<LeaveRequestAttachment>();

        // Audit / soft delete
        public DateTimeOffset? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}
