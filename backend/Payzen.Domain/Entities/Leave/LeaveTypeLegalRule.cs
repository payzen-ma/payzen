using System.ComponentModel.DataAnnotations;
using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Leave;

public class LeaveTypeLegalRule : BaseEntity
{
    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    [Required, MaxLength(50)]
    public string EventCaseCode { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    public int DaysGranted { get; set; }

    [Required, MaxLength(50)]
    public string LegalArticle { get; set; } = string.Empty;

    public bool CanBeDiscountinuous { get; set; } = false;
    public int? MustBeUsedWithinDays { get; set; }
}
