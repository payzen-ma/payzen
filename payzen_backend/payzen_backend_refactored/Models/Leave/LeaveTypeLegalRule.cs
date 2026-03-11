using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Leave
{
    public class LeaveTypeLegalRule
    {
        public int Id { get; set; }
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

        // Champs Audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}