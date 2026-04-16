using System.ComponentModel.DataAnnotations;
using Payzen.Domain.Enums;

namespace Payzen.Application.DTOs.Leave;

// ════════════════════════════════════════════════════════════
// LEAVE TYPE
// ════════════════════════════════════════════════════════════

public class LeaveTypeCreateDto
{
    [Required, MinLength(3), MaxLength(50)]
    public string LeaveCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LeaveName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string LeaveDescription { get; set; } = string.Empty;

    [Required]
    public LeaveScope Scope { get; set; } = LeaveScope.Global;

    public int? CompanyId { get; set; } // requis si Scope=Company

    public bool IsActive { get; set; } = true;
}

public class LeaveTypePatchDto
{
    public string? LeaveCode { get; set; }
    public string? LeaveName { get; set; }
    public string? LeaveDescription { get; set; }
    public LeaveScope? Scope { get; set; }
    public int? CompanyId { get; set; }
    public bool? IsActive { get; set; }
}

public class LeaveTypeReadDto
{
    public int Id { get; set; }
    public string LeaveCode { get; set; } = string.Empty;
    public string LeaveName { get; set; } = string.Empty;
    public string LeaveDescription { get; set; } = string.Empty;
    public LeaveScope Scope { get; set; }
    public int? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// LEAVE TYPE POLICY
// ════════════════════════════════════════════════════════════

public class LeaveTypePolicyCreateDto
{
    public int? CompanyId { get; set; } // null = policy globale

    [Required]
    public int LeaveTypeId { get; set; }

    public bool IsEnabled { get; set; } = true;
    public LeaveAccrualMethod AccrualMethod { get; set; } = LeaveAccrualMethod.Monthly;

    public decimal DaysPerMonthAdult { get; set; } = 1.50m;
    public decimal DaysPerMonthMinor { get; set; } = 2.00m;
    public decimal BonusDaysPerYearAfter5Years { get; set; } = 1.50m;

    public bool RequiresEligibility6Months { get; set; } = false;
    public bool RequiresBalance { get; set; }

    public int AnnualCapDays { get; set; } = 30;
    public bool AllowCarryover { get; set; } = true;
    public int MaxCarryoverYears { get; set; } = 2;
    public int MinConsecutiveDays { get; set; } = 12;
    public bool UseWorkingCalendar { get; set; } = true;
}

public class LeaveTypePolicyPatchDto
{
    public bool? IsEnabled { get; set; }
    public LeaveAccrualMethod? AccrualMethod { get; set; }
    public decimal? DaysPerMonthAdult { get; set; }
    public decimal? DaysPerMonthMinor { get; set; }
    public decimal? BonusDaysPerYearAfter5Years { get; set; }
    public bool? RequiresEligibility6Months { get; set; }
    public bool? RequiresBalance { get; set; }
    public int? AnnualCapDays { get; set; }
    public bool? AllowCarryover { get; set; }
    public int? MaxCarryoverYears { get; set; }
    public int? MinConsecutiveDays { get; set; }
    public bool? UseWorkingCalendar { get; set; }
}

public class LeaveTypePolicyReadDto
{
    public int Id { get; set; }
    public int? CompanyId { get; set; }
    public int LeaveTypeId { get; set; }
    public bool IsEnabled { get; set; }
    public LeaveAccrualMethod AccrualMethod { get; set; }
    public decimal DaysPerMonthAdult { get; set; }
    public decimal DaysPerMonthMinor { get; set; }
    public bool RequiresEligibility6Months { get; set; }
    public bool RequiresBalance { get; set; }
    public decimal BonusDaysPerYearAfter5Years { get; set; }
    public int AnnualCapDays { get; set; }
    public bool AllowCarryover { get; set; }
    public int MaxCarryoverYears { get; set; }
    public int MinConsecutiveDays { get; set; }
    public bool UseWorkingCalendar { get; set; }
}

// ════════════════════════════════════════════════════════════
// LEAVE TYPE LEGAL RULE
// ════════════════════════════════════════════════════════════

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

// ════════════════════════════════════════════════════════════
// LEAVE REQUEST
// ════════════════════════════════════════════════════════════

public class LeaveRequestCreateDto
{
    [Required]
    public int LeaveTypeId { get; set; }

    /// <summary>Si renseigné (valeur positive), politique explicite ; sinon résolution par entreprise et type.</summary>
    public int? LeaveTypePolicyId { get; set; }

    //Requis si le type nécessite un cas légal (ex: MARRIAGE)
    public int? LegalRuleId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [MaxLength(1000)]
    public string? EmployeeNote { get; set; }
}

public class LeaveRequestPatchDto
{
    public int? LeaveTypeId { get; set; }
    public int? LeaveTypePolicyId { get; set; }
    public int? LegalRuleId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool? IsRenounced { get; set; }

    [MaxLength(1000)]
    public string? EmployeeNote { get; set; }

    [MaxLength(1000)]
    public string? ManagerNote { get; set; }
}

/// <summary>Corps JSON pour approve / reject / cancel (aligné avec ApprovalDto côté Angular).</summary>
public class LeaveRequestDecisionDto
{
    public string? Comment { get; set; }
    public string? ApproverNotes { get; set; }
}

public class LeaveRequestReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int CompanyId { get; set; }

    public int LeaveTypeId { get; set; }
    public string LeaveTypeCode { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;

    public int LeaveTypePolicyId { get; set; }
    public string LevaeTypePolicyCode { get; set; } = string.Empty; // note: typo conservée du source
    public string LeaveTypePolicyName { get; set; } = string.Empty;

    public int? LegalRuleId { get; set; }
    public string? LegalCaseCode { get; set; }
    public string? LegalCaseDescription { get; set; }
    public int? LegalDaysGranted { get; set; }
    public string? LegalArticle { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public LeaveRequestStatus Status { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? DecisionAt { get; set; }
    public int? DecisionBy { get; set; }
    public string? DecisionComment { get; set; }

    public int CalendarDays { get; set; }
    public decimal WorkingDaysDeducted { get; set; }
    public bool HasMinConsecutiveBlock { get; set; }
    public string? ComputationVersion { get; set; }

    public bool IsRenounced { get; set; }
    public string? EmployeeNote { get; set; }
    public string? ManagerNote { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// LEAVE REQUEST APPROVAL HISTORY
// ════════════════════════════════════════════════════════════

public class LeaveRequestApprovalHistoryCreateDto
{
    [Required]
    public int LeaveRequestId { get; set; }

    [Required]
    public LeaveApprovalAction Action { get; set; }

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

// ════════════════════════════════════════════════════════════
// LEAVE REQUEST ATTACHMENT
// ════════════════════════════════════════════════════════════

public class LeaveRequestAttachmentCreateDto
{
    [Required]
    public int LeaveRequestId { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FileType { get; set; }

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

// ════════════════════════════════════════════════════════════
// LEAVE REQUEST EXEMPTION
// ════════════════════════════════════════════════════════════

public class LeaveRequestExemptionCreateDto
{
    [Required]
    public int LeaveRequestId { get; set; }

    [Required]
    public DateOnly ExemptionDate { get; set; }

    [Required]
    public LeaveExemptionReasonType ReasonType { get; set; }

    //Par défaut : la date est exclue du congé (CountsAsLeaveDay=false)
    public bool CountsAsLeaveDay { get; set; } = false;

    public int? HolidayId { get; set; }
    public int? EmployeeAbsenceId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public class LeaveRequestExemptionPatchDto
{
    public DateOnly? ExemptionDate { get; set; }
    public LeaveExemptionReasonType? ReasonType { get; set; }
    public bool? CountsAsLeaveDay { get; set; }
    public int? HolidayId { get; set; }
    public int? EmployeeAbsenceId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public class LeaveRequestExemptionReadDto
{
    public int Id { get; set; }
    public int LeaveRequestId { get; set; }
    public DateOnly ExemptionDate { get; set; }
    public LeaveExemptionReasonType ReasonType { get; set; }
    public bool CountsAsLeaveDay { get; set; }
    public int? HolidayId { get; set; }
    public int? EmployeeAbsenceId { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
}

// ════════════════════════════════════════════════════════════
// LEAVE BALANCE
// ════════════════════════════════════════════════════════════

public class LeaveBalanceCreateDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int LeaveTypeId { get; set; }

    [Required]
    public int Year { get; set; }

    [Required, Range(1, 12)]
    public int Month { get; set; }

    public decimal OpeningDays { get; set; } = 0m;
    public decimal AccruedDays { get; set; } = 0m;
    public decimal UsedDays { get; set; } = 0m;
    public decimal CarryInDays { get; set; } = 0m;
    public decimal CarryOutDays { get; set; } = 0m;
    public decimal ClosingDays { get; set; } = 0m;

    public DateOnly? CarryoverExpiresOn { get; set; }
    public DateTimeOffset? LastRecalculatedAt { get; set; }
}

public class LeaveBalancePatchDto
{
    public decimal? OpeningDays { get; set; }
    public decimal? AccruedDays { get; set; }
    public decimal? UsedDays { get; set; }
    public decimal? CarryInDays { get; set; }
    public decimal? CarryOutDays { get; set; }
    public decimal? ClosingDays { get; set; }
    public DateOnly? CarryoverExpiresOn { get; set; }
    public DateTimeOffset? LastRecalculatedAt { get; set; }
}

public class LeaveBalanceReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int CompanyId { get; set; }
    public int LeaveTypeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    //Date d'expiration du solde (fin du mois + 2 ans).
    public DateOnly? BalanceExpiresOn { get; set; }
    public decimal OpeningDays { get; set; }
    public decimal AccruedDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal CarryInDays { get; set; }
    public decimal CarryOutDays { get; set; }
    public decimal ClosingDays { get; set; }
    public DateOnly? CarryoverExpiresOn { get; set; }

    //True si le report est expiré à la date de référence.
    public bool IsCarryoverExpired { get; set; }
    public DateTimeOffset? LastRecalculatedAt { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed record LeaveBalanceRecalculateResultDto(int RecalculatedCount, int TotalBalances, string Message);

// ════════════════════════════════════════════════════════════
// LEAVE CARRY OVER AGREEMENT
// ════════════════════════════════════════════════════════════

public class LeaveCarryOverAgreementCreateDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int LeaveTypeId { get; set; }

    [Required]
    public int FromYear { get; set; }

    [Required]
    public int ToYear { get; set; }

    [Required]
    public DateOnly AgreementDate { get; set; }

    [MaxLength(500)]
    public string? AgreementDocRef { get; set; }
}

public class LeaveCarryOverAgreementPatchDto
{
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }
    public DateOnly? AgreementDate { get; set; }

    [MaxLength(500)]
    public string? AgreementDocRef { get; set; }
}

public class LeaveCarryOverAgreementReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int CompanyId { get; set; }
    public int LeaveTypeId { get; set; }
    public int FromYear { get; set; }
    public int ToYear { get; set; }
    public DateOnly AgreementDate { get; set; }
    public string? AgreementDocRef { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// LEAVE AUDIT LOG
// ════════════════════════════════════════════════════════════

public class LeaveAuditLogCreateDto
{
    [Required]
    public int CompanyId { get; set; }

    public int? EmployeeId { get; set; }
    public int? LeaveRequestId { get; set; }

    [Required, MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? OldValue { get; set; }

    [MaxLength(2000)]
    public string? NewValue { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
}

public class LeaveAuditLogPatchDto
{
    [MaxLength(200)]
    public string? EventName { get; set; }

    [MaxLength(2000)]
    public string? OldValue { get; set; }

    [MaxLength(2000)]
    public string? NewValue { get; set; }
}

public class LeaveAuditLogReadDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int? EmployeeId { get; set; }
    public int? LeaveRequestId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}
