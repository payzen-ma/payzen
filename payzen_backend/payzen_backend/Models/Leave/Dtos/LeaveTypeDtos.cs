using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Models.Leave.Dtos
{
    public class LeaveTypeCreateDto
    {
        [Required, MinLength(3), MaxLength(50)]
        public string LeaveCode { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LeaveName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string LeaveDescription { get; set; } = string.Empty;

        [Required]
        public LeaveScope Scope { get; set; } = LeaveScope.Global;

        public int? CompanyId { get; set; } // requis si Scope=Company

        public bool IsActive { get; set; } = true;
    }

    public class LeaveTypePatchDto
    {
        public string? LeaveCode { get; set; }
        public string? LeaveName { get; set; }
        public string? LeaveDescription { get; set; }

        public LeaveScope? Scope { get; set; }
        public int? CompanyId { get; set; }

        public bool? IsActive { get; set; }
    }

    public class LeaveTypeReadDto
    {
        public int Id { get; set; }
        public string LeaveCode { get; set; } = string.Empty;
        public string LeaveName { get; set; } = string.Empty;
        public string LeaveDescription { get; set; } = string.Empty;

        public LeaveScope Scope { get; set; }
        public int? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
    }
}
