using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Models.Leave
{
    public class LeaveRequestApprovalHistory
    {
        public int Id { get; set; }

        public int LeaveRequestId { get; set; }
        public LeaveRequest LeaveRequest { get; set; } = null!;

        public LeaveApprovalAction Action { get; set; }
        public DateTimeOffset ActionAt { get; set; }
        public int ActionBy { get; set; } // Users.Id

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
