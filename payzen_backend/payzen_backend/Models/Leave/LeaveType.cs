using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Models.Leave
{
    public class LeaveType
    {
        public int Id { get; set; }
        
        [Required, MaxLength(50)]
        public string LeaveCode { get; set; } = string.Empty;
        
        [Required, MaxLength(100)]
        public string LeaveNameAr { get; set; } = string.Empty;
        
        [Required, MaxLength(100)]
        public string LeaveNameEn { get; set; } = string.Empty;
        
        [Required, MaxLength(100)]
        public string LeaveNameFr { get; set; } = string.Empty;
        
        [Required, MaxLength(500)]
        public string LeaveDescription { get; set; } = string.Empty;

        public LeaveScope Scope { get; set; } = LeaveScope.Global;

        public bool IsActive {get; set;} = true;

        // Null si Global (Légal), sinon CompanyId propriétaire (Scope = Company)
        public int? CompanyId { get; set; }

        // Audit fields
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        // Navigation properties
        public Company.Company? Company { get; set; }
        public ICollection<LeaveTypeLegalRule> LegalRules { get; set; } = new List<LeaveTypeLegalRule>();
        public ICollection<LeaveTypePolicy> Policies { get; set; } = new List<LeaveTypePolicy>();
    }
}
