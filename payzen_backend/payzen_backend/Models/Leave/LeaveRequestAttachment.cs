using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Leave
{
    public class LeaveRequestAttachment
    {
        public int Id { get; set; }

        public int LeaveRequestId { get; set; }
        public LeaveRequest LeaveRequest { get; set; } = null!;

        [Required, MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FileType { get; set; }

        public DateTimeOffset UploadedAt { get; set; }
        public int UploadedBy { get; set; } // Users.Id
    }
}
