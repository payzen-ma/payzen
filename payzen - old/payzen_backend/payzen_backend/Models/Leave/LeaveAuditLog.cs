using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Leave
{
    public class LeaveAuditLog
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public Company.Company Company { get; set; } = null!;

        public int? EmployeeId { get; set; }
        public Employee.Employee? Employee { get; set; }

        public int? LeaveRequestId { get; set; }
        public LeaveRequest? LeaveRequest { get; set; }

        [Required, MaxLength(200)]
        public string EventName { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? OldValue { get; set; }

        [MaxLength(2000)]
        public string? NewValue { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public int CreatedBy { get; set; } // Users.Id
    }
}
