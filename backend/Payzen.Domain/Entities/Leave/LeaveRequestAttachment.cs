using Payzen.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Payzen.Domain.Entities.Leave;

public class LeaveRequestAttachment : BaseEntity
{

    public int LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;

    [Required, MaxLength(255)]  public string FileName { get; set; } = string.Empty;
    [Required, MaxLength(1000)] public string FilePath { get; set; } = string.Empty;
    [MaxLength(100)]            public string? FileType { get; set; }
}
