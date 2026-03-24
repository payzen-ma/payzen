using Payzen.Application.Common;
using Payzen.Application.DTOs.Leave;
using Payzen.Domain.Enums;

namespace Payzen.Application.Interfaces;

/// <summary>
/// Gestion complète des congés : types, politiques, règles légales, demandes,
/// soldes, carry-over, pièces jointes, exemptions, audit logs.
/// </summary>
public interface ILeaveService
{
    // ── LeaveType ────────────────────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<LeaveTypeReadDto>>> GetLeaveTypesAsync(int? companyId, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypeReadDto>> GetLeaveTypeByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypeReadDto>> CreateLeaveTypeAsync(LeaveTypeCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypeReadDto>> PatchLeaveTypeAsync(int id, LeaveTypePatchDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteLeaveTypeAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── LeaveTypePolicy ──────────────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<LeaveTypePolicyReadDto>>> GetPoliciesAsync(int? companyId, int? leaveTypeId, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypePolicyReadDto>> GetPolicyByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypePolicyReadDto>> CreatePolicyAsync(LeaveTypePolicyCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypePolicyReadDto>> PatchPolicyAsync(int id, LeaveTypePolicyPatchDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeletePolicyAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── LeaveTypeLegalRule ───────────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<LeaveTypeLegalRuleReadDto>>> GetLegalRulesAsync(int? leaveTypeId, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypeLegalRuleReadDto>> GetLegalRuleByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypeLegalRuleReadDto>> CreateLegalRuleAsync(LeaveTypeLegalRuleCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypeLegalRuleReadDto>> PatchLegalRuleAsync(int id, LeaveTypeLegalRulePatchDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteLegalRuleAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── LeaveRequest ─────────────────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<LeaveRequestReadDto>>> GetLeaveRequestsAsync(int? companyId, int? employeeId, LeaveRequestStatus? status, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<LeaveRequestReadDto>>> GetLeaveRequestsByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestReadDto>> GetLeaveRequestByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<LeaveRequestReadDto>>> GetPendingApprovalAsync(int? companyId, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestReadDto>> CreateLeaveRequestForSelfAsync(LeaveRequestCreateDto dto, int userId, CancellationToken ct = default);
    Task<ServiceResult> CreateLeaveRequestForOtherEmployeeAsync(int targetEmployeeId, LeaveRequestCreateDto dto, int actorUserId, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestReadDto>> CreateLeaveRequestAsync(int employeeId, LeaveRequestCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestReadDto>> PatchLeaveRequestAsync(int id, LeaveRequestPatchDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestReadDto>> PutLeaveRequestAsync(int id, LeaveRequestPatchDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestReadDto>> SubmitLeaveRequestAsync(int id, int userId, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestReadDto>> ApproveLeaveRequestAsync(int id, string? comment, int decidedBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestReadDto>> RejectLeaveRequestAsync(int id, string? comment, int decidedBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestReadDto>> CancelLeaveRequestAsync(int id, string? comment, int userId, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestReadDto>> RenounceLeaveRequestAsync(int id, int userId, CancellationToken ct = default);
    Task<ServiceResult> DeleteLeaveRequestAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── LeaveBalance ─────────────────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<LeaveBalanceReadDto>>> GetBalancesAsync(int employeeId, int? leaveTypeId, CancellationToken ct = default);
    /// <summary>Filtres optionnels (comme l'ancien GET api/leave-balances). Au moins un critère recommandé pour éviter des listes énormes.</summary>
    Task<ServiceResult<IEnumerable<LeaveBalanceReadDto>>> GetBalancesFilteredAsync(int? companyId, int? employeeId, int? year, int? month, int? leaveTypeId, CancellationToken ct = default);
    Task<ServiceResult<LeaveBalanceReadDto>> GetBalanceByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<LeaveBalanceReadDto>>> GetBalancesByYearAsync(int employeeId, int year, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<LeaveBalanceReadDto>>> GetBalancesByYearMonthAsync(int employeeId, int year, int month, CancellationToken ct = default);
    Task<ServiceResult<object>> GetBalanceSummaryAsync(int employeeId, int? companyId, CancellationToken ct = default);
    Task<ServiceResult<LeaveBalanceRecalculateResultDto>> RecalculateBalancesForMonthAsync(int employeeId, int year, int month, int? companyId, int? leaveTypeId, int userId, CancellationToken ct = default);
    Task<ServiceResult<LeaveBalanceReadDto>> CreateBalanceAsync(LeaveBalanceCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveBalanceReadDto>> PatchBalanceAsync(int id, LeaveBalancePatchDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteBalanceAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── LeaveCarryOverAgreement ──────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<LeaveCarryOverAgreementReadDto>>> GetCarryOverAgreementsAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<LeaveCarryOverAgreementReadDto>> GetCarryOverByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<LeaveCarryOverAgreementReadDto>> CreateCarryOverAgreementAsync(LeaveCarryOverAgreementCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveCarryOverAgreementReadDto>> PatchCarryOverAgreementAsync(int id, LeaveCarryOverAgreementPatchDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteCarryOverAgreementAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── LeaveRequestAttachment ───────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<LeaveRequestAttachmentReadDto>>> GetAttachmentsAsync(int leaveRequestId, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestAttachmentReadDto>> GetAttachmentByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestAttachmentReadDto>> CreateAttachmentAsync(LeaveRequestAttachmentCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<(byte[] content, string fileName, string contentType)>> GetAttachmentDownloadAsync(int attachmentId, CancellationToken ct = default);
    Task<ServiceResult> DeleteAttachmentAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── LeaveRequestExemption ────────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<LeaveRequestExemptionReadDto>>> GetExemptionsAsync(int leaveRequestId, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestExemptionReadDto>> GetExemptionByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestExemptionReadDto>> CreateExemptionAsync(LeaveRequestExemptionCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestExemptionReadDto>> PatchExemptionAsync(int id, LeaveRequestExemptionPatchDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteExemptionAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── LeaveAuditLog ────────────────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<LeaveAuditLogReadDto>>> GetAuditLogsAsync(int? companyId, int? leaveRequestId, CancellationToken ct = default);
    Task<ServiceResult<LeaveAuditLogReadDto>> GetAuditLogByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<LeaveAuditLogReadDto>>> GetAuditLogsByEmployeeAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<LeaveAuditLogReadDto>> CreateAuditLogAsync(LeaveAuditLogCreateDto dto, CancellationToken ct = default);
}