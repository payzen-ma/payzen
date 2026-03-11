using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Leave.Dtos
{
    public class LeaveAuditLogCreateDto
    {
        [Required] public int CompanyId { get; set; }

        public int? EmployeeId { get; set; }
        public int? LeaveRequestId { get; set; }

        [Required, MaxLength(200)]
        public string EventName { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? OldValue { get; set; }

        [MaxLength(2000)]
        public string? NewValue { get; set; }

        // généralement côté serveur
        public DateTimeOffset? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class LeaveAuditLogPatchDto
    {
        [MaxLength(200)]
        public string? EventName { get; set; }

        [MaxLength(2000)]
        public string? OldValue { get; set; }

        [MaxLength(2000)]
        public string? NewValue { get; set; }
    }

    public class LeaveAuditLogReadDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int? EmployeeId { get; set; }
        public int? LeaveRequestId { get; set; }

        public string EventName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public int CreatedBy { get; set; }
    }
}
