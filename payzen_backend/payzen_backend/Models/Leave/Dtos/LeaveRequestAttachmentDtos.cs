using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Leave.Dtos
{
    public class LeaveRequestAttachmentCreateDto
    {
        [Required] public int LeaveRequestId { get; set; }

        [Required, MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FileType { get; set; }

        // UploadedAt/By peuvent être set côté serveur
        public DateTimeOffset? UploadedAt { get; set; }
        public int? UploadedBy { get; set; }
    }

    public class LeaveRequestAttachmentPatchDto
    {
        [MaxLength(255)]
        public string? FileName { get; set; }

        [MaxLength(1000)]
        public string? FilePath { get; set; }

        [MaxLength(100)]
        public string? FileType { get; set; }
    }

    public class LeaveRequestAttachmentReadDto
    {
        public int Id { get; set; }
        public int LeaveRequestId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? FileType { get; set; }

        public DateTimeOffset UploadedAt { get; set; }
        public int UploadedBy { get; set; }
    }
}