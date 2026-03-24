using System.ComponentModel.DataAnnotations;
using Payzen.Domain.Enums;
using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Leave;

public class LeaveType : BaseEntity
{
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
    public bool IsActive { get; set; } = true;
    public int? CompanyId { get; set; }

    // Navigation properties
    public Company.Company? Company { get; set; }
    public ICollection<LeaveTypeLegalRule> LegalRules { get; set; } = new List<LeaveTypeLegalRule>();
    public ICollection<LeaveTypePolicy> Policies { get; set; } = new List<LeaveTypePolicy>();
}
