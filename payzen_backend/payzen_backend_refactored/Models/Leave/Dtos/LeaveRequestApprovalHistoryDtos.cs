using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Models.Leave.Dtos
{
    public class LeaveRequestApprovalHistoryCreateDto
    {
        [Required] public int LeaveRequestId { get; set; }
        [Required] public LeaveApprovalAction Action { get; set; }

        // ActionAt/By normalement côté serveur (utilisateur courant + now)
        public DateTimeOffset? ActionAt { get; set; }
        public int? ActionBy { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    public class LeaveRequestApprovalHistoryPatchDto
    {
        public LeaveApprovalAction? Action { get; set; }
        public DateTimeOffset? ActionAt { get; set; }
        public int? ActionBy { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    public class LeaveRequestApprovalHistoryReadDto
    {
        public int Id { get; set; }
        public int LeaveRequestId { get; set; }
        public LeaveApprovalAction Action { get; set; }
        public DateTimeOffset ActionAt { get; set; }
        public int ActionBy { get; set; }
        public string? Comment { get; set; }
    }
}