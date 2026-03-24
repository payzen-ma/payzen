using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Leave.Dtos
{
    public class LeaveTypeLegalRuleCreateDto
    {
        [Required]
        public int LeaveTypeId { get; set; }

        [Required, MaxLength(50)]
        public string EventCaseCode { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        [Range(1, 3650)]
        public int DaysGranted { get; set; }

        [Required, MaxLength(50)]
        public string LegalArticle { get; set; } = string.Empty;

        public bool CanBeDiscontinuous { get; set; } = false;

        [Range(1, 3650)]
        public int? MustBeUsedWithinDays { get; set; }
    }

    public class LeaveTypeLegalRulePatchDto
    {
        public string? EventCaseCode { get; set; }
        public string? Description { get; set; }
        public int? DaysGranted { get; set; }
        public string? LegalArticle { get; set; }
        public bool? CanBeDiscontinuous { get; set; }
        public int? MustBeUsedWithinDays { get; set; }
    }

    public class LeaveTypeLegalRuleReadDto
    {
        public int Id { get; set; }
        public int LeaveTypeId { get; set; }
        public string EventCaseCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DaysGranted { get; set; }
        public string LegalArticle { get; set; } = string.Empty;
        public bool CanBeDiscontinuous { get; set; }
        public int? MustBeUsedWithinDays { get; set; }
    }
}
