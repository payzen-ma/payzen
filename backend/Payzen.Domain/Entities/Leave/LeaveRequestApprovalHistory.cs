using Payzen.Domain.Common;
using System.ComponentModel.DataAnnotations;
using Payzen.Domain.Enums;

namespace Payzen.Domain.Entities.Leave;

public class LeaveRequestApprovalHistory : BaseEntity
{

    public int LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;

    public LeaveApprovalAction Action { get; set; }
    public DateTimeOffset ActionAt { get; set; }
    public int ActionBy { get; set; }

    [MaxLength(1000)] public string? Comment { get; set; }
}
