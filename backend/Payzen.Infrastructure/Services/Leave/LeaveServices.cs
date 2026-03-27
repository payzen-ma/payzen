using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Leave;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Auth;
using Payzen.Domain.Entities.Leave;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Leave;

public class LeaveService : ILeaveService
{
    private static class LeaveLog
    {
        public const string RequestCreated   = "LeaveRequest_Created";
        public const string RequestUpdated   = "LeaveRequest_Updated";
        public const string RequestDeleted   = "LeaveRequest_Deleted";
        public const string RequestSubmitted = "LeaveRequest_Submitted";
        public const string RequestApproved  = "LeaveRequest_Approved";
        public const string RequestRejected  = "LeaveRequest_Rejected";
        public const string RequestCancelled = "LeaveRequest_Cancelled";
        public const string RequestRenounced = "LeaveRequest_Renounced";
    }

    private readonly AppDbContext _db;
    private readonly IWorkingDaysCalculator _workingDays;
    private readonly ILeaveEventLogService _leaveEventLog;
    private readonly ILeaveBalanceRecalculationService _leaveBalanceRecalc;

    public LeaveService(
        AppDbContext db,
        IWorkingDaysCalculator workingDays,
        ILeaveEventLogService leaveEventLog,
        ILeaveBalanceRecalculationService leaveBalanceRecalc)
    {
        _db = db;
        _workingDays = workingDays;
        _leaveEventLog = leaveEventLog;
        _leaveBalanceRecalc = leaveBalanceRecalc;
    }

    // ── LeaveType ─────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<LeaveTypeReadDto>>> GetLeaveTypesAsync(int? companyId, CancellationToken ct = default)
    {
        var q = _db.LeaveTypes.AsNoTracking().Where(lt => lt.DeletedAt == null);
        if (companyId.HasValue)
            q = q.Where(lt => lt.Scope == LeaveScope.Global || lt.CompanyId == companyId.Value);
        else
            q = q.Where(lt => lt.Scope == LeaveScope.Global);

        var raw = await q
            .Include(lt => lt.Company)
            .OrderBy(lt => lt.LeaveCode)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveTypeReadDto>>.Ok(raw.Select(MapLeaveType).ToList());
    }

    public async Task<ServiceResult<LeaveTypeReadDto>> GetLeaveTypeByIdAsync(int id, CancellationToken ct = default)
    {
        var lt = await _db.LeaveTypes.AsNoTracking()
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        return lt == null
            ? ServiceResult<LeaveTypeReadDto>.Fail("Type de congé introuvable.")
            : ServiceResult<LeaveTypeReadDto>.Ok(MapLeaveType(lt));
    }

    public async Task<ServiceResult<LeaveTypeReadDto>> CreateLeaveTypeAsync(LeaveTypeCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        if (dto.Scope == LeaveScope.Company && dto.CompanyId == null)
            return ServiceResult<LeaveTypeReadDto>.Fail("CompanyId est obligatoire pour les types de congés d'entreprise (Scope=Company).");
        if (dto.Scope == LeaveScope.Global && dto.CompanyId != null)
            return ServiceResult<LeaveTypeReadDto>.Fail("CompanyId doit être null pour les types de congés globaux (Scope=Global).");

        // Vérification unicité du LeaveCode
        bool codeExists;
        if (dto.Scope == LeaveScope.Global)
            codeExists = await _db.LeaveTypes.AnyAsync(lt => lt.LeaveCode == dto.LeaveCode && lt.Scope == LeaveScope.Global && lt.DeletedAt == null, ct);
        else
            codeExists = await _db.LeaveTypes.AnyAsync(lt => lt.LeaveCode == dto.LeaveCode && lt.CompanyId == dto.CompanyId && lt.DeletedAt == null, ct);

        if (codeExists)
            return ServiceResult<LeaveTypeReadDto>.Fail($"Un type de congé avec le code '{dto.LeaveCode}' existe déjà dans ce contexte.");

        var lt = new LeaveType
        {
            LeaveCode = dto.LeaveCode.Trim(),
            LeaveNameFr = dto.LeaveName.Trim(),
            LeaveNameAr = dto.LeaveName.Trim(),
            LeaveNameEn = dto.LeaveName.Trim(),
            LeaveDescription = dto.LeaveDescription?.Trim() ?? string.Empty,
            Scope = dto.Scope,
            CompanyId = dto.CompanyId,
            IsActive = dto.IsActive,
            CreatedBy = createdBy
        };
        _db.LeaveTypes.Add(lt);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(lt).Reference(x => x.Company).LoadAsync(ct);
        return ServiceResult<LeaveTypeReadDto>.Ok(MapLeaveType(lt));
    }

    public async Task<ServiceResult<LeaveTypeReadDto>> PatchLeaveTypeAsync(int id, LeaveTypePatchDto dto, int updatedBy, CancellationToken ct = default)
    {
        var lt = await _db.LeaveTypes
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (lt == null) return ServiceResult<LeaveTypeReadDto>.Fail("Type de congé introuvable.");

        // Vérification unicité si LeaveCode change
        if (dto.LeaveCode != null && dto.LeaveCode != lt.LeaveCode)
        {
            bool codeExists;
            if (lt.Scope == LeaveScope.Global)
                codeExists = await _db.LeaveTypes.AnyAsync(x => x.Id != id && x.LeaveCode == dto.LeaveCode && x.Scope == LeaveScope.Global && x.DeletedAt == null, ct);
            else
                codeExists = await _db.LeaveTypes.AnyAsync(x => x.Id != id && x.LeaveCode == dto.LeaveCode && x.CompanyId == lt.CompanyId && x.DeletedAt == null, ct);
            if (codeExists)
                return ServiceResult<LeaveTypeReadDto>.Fail($"Un type de congé avec le code '{dto.LeaveCode}' existe déjà.");
            lt.LeaveCode = dto.LeaveCode.Trim();
        }

        if (dto.LeaveName != null)
        {
            lt.LeaveNameFr = dto.LeaveName.Trim();
            lt.LeaveNameAr = dto.LeaveName.Trim();
            lt.LeaveNameEn = dto.LeaveName.Trim();
        }
        if (dto.LeaveDescription != null) lt.LeaveDescription = dto.LeaveDescription.Trim();
        if (dto.Scope != null) lt.Scope = dto.Scope.Value;
        if (dto.CompanyId != null) lt.CompanyId = dto.CompanyId;
        if (dto.IsActive != null) lt.IsActive = dto.IsActive.Value;
        lt.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        await _db.Entry(lt).Reference(x => x.Company).LoadAsync(ct);
        return ServiceResult<LeaveTypeReadDto>.Ok(MapLeaveType(lt));
    }

    public async Task<ServiceResult> DeleteLeaveTypeAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var lt = await _db.LeaveTypes.FindAsync(new object[] { id }, ct);
        if (lt == null) return ServiceResult.Fail("Type de congé introuvable.");

        // Vérification dépendances
        var hasRequests = await _db.LeaveRequests.AnyAsync(lr => lr.LeaveTypeId == id && lr.DeletedAt == null, ct);
        if (hasRequests)
            return ServiceResult.Fail("Impossible de supprimer ce type de congé car il est utilisé dans des demandes de congés.");

        var hasBalances = await _db.LeaveBalances.AnyAsync(lb => lb.LeaveTypeId == id && lb.DeletedAt == null, ct);
        if (hasBalances)
            return ServiceResult.Fail("Impossible de supprimer ce type de congé car il est utilisé dans des soldes de congés.");

        lt.DeletedAt = DateTimeOffset.UtcNow;
        lt.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── LeaveTypePolicy ───────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<LeaveTypePolicyReadDto>>> GetPoliciesAsync(int? companyId, int? leaveTypeId, CancellationToken ct = default)
    {
        var q = _db.LeaveTypePolicies.AsNoTracking().Where(p => p.DeletedAt == null);
        if (companyId.HasValue) q = q.Where(p => p.CompanyId == companyId.Value);
        if (leaveTypeId.HasValue) q = q.Where(p => p.LeaveTypeId == leaveTypeId.Value);

        var raw = await q.OrderBy(p => p.CompanyId).ThenBy(p => p.LeaveTypeId).ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveTypePolicyReadDto>>.Ok(raw.Select(MapPolicy).ToList());
    }

    public async Task<ServiceResult<LeaveTypePolicyReadDto>> GetPolicyByIdAsync(int id, CancellationToken ct = default)
    {
        var p = await _db.LeaveTypePolicies.FindAsync(new object[] { id }, ct);
        return p == null ? ServiceResult<LeaveTypePolicyReadDto>.Fail("Politique introuvable.") : ServiceResult<LeaveTypePolicyReadDto>.Ok(MapPolicy(p));
    }

    public async Task<ServiceResult<LeaveTypePolicyReadDto>> CreatePolicyAsync(LeaveTypePolicyCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        // Vérification LeaveType existe
        var leaveTypeExists = await _db.LeaveTypes.AnyAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null, ct);
        if (!leaveTypeExists)
            return ServiceResult<LeaveTypePolicyReadDto>.Fail("Type de congé non trouvé.");

        // Unicité: une seule policy par (CompanyId, LeaveTypeId)
        var policyExists = await _db.LeaveTypePolicies.AnyAsync(p => p.CompanyId == dto.CompanyId && p.LeaveTypeId == dto.LeaveTypeId && p.DeletedAt == null, ct);
        if (policyExists)
            return ServiceResult<LeaveTypePolicyReadDto>.Fail("Une politique existe déjà pour ce type de congé et cette entreprise.");

        if (dto.MinConsecutiveDays < 0)
            return ServiceResult<LeaveTypePolicyReadDto>.Fail("MinConsecutiveDays doit être supérieur ou égal à 0.");

        var p = new LeaveTypePolicy
        {
            CompanyId = dto.CompanyId,
            LeaveTypeId = dto.LeaveTypeId,
            IsEnabled = dto.IsEnabled,
            AccrualMethod = dto.AccrualMethod,
            DaysPerMonthAdult = dto.DaysPerMonthAdult,
            DaysPerMonthMinor = dto.DaysPerMonthMinor,
            BonusDaysPerYearAfter5Years = dto.BonusDaysPerYearAfter5Years,
            RequiresEligibility6Months = dto.RequiresEligibility6Months,
            RequiresBalance = dto.RequiresBalance,
            AnnualCapDays = dto.AnnualCapDays,
            AllowCarryover = dto.AllowCarryover,
            MaxCarryoverYears = dto.MaxCarryoverYears,
            MinConsecutiveDays = dto.MinConsecutiveDays,
            UseWorkingCalendar = dto.UseWorkingCalendar,
            CreatedBy = createdBy
        };
        _db.LeaveTypePolicies.Add(p);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveTypePolicyReadDto>.Ok(MapPolicy(p));
    }

    public async Task<ServiceResult<LeaveTypePolicyReadDto>> PatchPolicyAsync(int id, LeaveTypePolicyPatchDto dto, int updatedBy, CancellationToken ct = default)
    {
        var p = await _db.LeaveTypePolicies.FindAsync(new object[] { id }, ct);
        if (p == null) return ServiceResult<LeaveTypePolicyReadDto>.Fail("Politique introuvable.");

        if (dto.IsEnabled != null) p.IsEnabled = dto.IsEnabled.Value;
        if (dto.AccrualMethod != null) p.AccrualMethod = dto.AccrualMethod.Value;
        if (dto.DaysPerMonthAdult != null) p.DaysPerMonthAdult = dto.DaysPerMonthAdult.Value;
        if (dto.DaysPerMonthMinor != null) p.DaysPerMonthMinor = dto.DaysPerMonthMinor.Value;
        if (dto.BonusDaysPerYearAfter5Years != null) p.BonusDaysPerYearAfter5Years = dto.BonusDaysPerYearAfter5Years.Value;
        if (dto.AnnualCapDays != null) p.AnnualCapDays = dto.AnnualCapDays.Value;
        if (dto.AllowCarryover != null) p.AllowCarryover = dto.AllowCarryover.Value;
        if (dto.MaxCarryoverYears != null) p.MaxCarryoverYears = dto.MaxCarryoverYears.Value;
        if (dto.MinConsecutiveDays != null)
        {
            if (dto.MinConsecutiveDays < 0)
                return ServiceResult<LeaveTypePolicyReadDto>.Fail("MinConsecutiveDays doit être supérieur ou égal à 0.");
            p.MinConsecutiveDays = dto.MinConsecutiveDays.Value;
        }
        if (dto.RequiresEligibility6Months != null) p.RequiresEligibility6Months = dto.RequiresEligibility6Months.Value;
        if (dto.RequiresBalance != null) p.RequiresBalance = dto.RequiresBalance.Value;
        if (dto.UseWorkingCalendar != null) p.UseWorkingCalendar = dto.UseWorkingCalendar.Value;

        p.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveTypePolicyReadDto>.Ok(MapPolicy(p));
    }

    public async Task<ServiceResult> DeletePolicyAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var p = await _db.LeaveTypePolicies.FindAsync(new object[] { id }, ct);
        if (p == null) return ServiceResult.Fail("Politique introuvable.");
        p.DeletedAt = DateTimeOffset.UtcNow;
        p.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── LeaveTypeLegalRule ────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<LeaveTypeLegalRuleReadDto>>> GetLegalRulesAsync(int? leaveTypeId, CancellationToken ct = default)
    {
        var q = _db.LeaveTypeLegalRules.AsNoTracking().Where(r => r.DeletedAt == null);
        if (leaveTypeId.HasValue) q = q.Where(r => r.LeaveTypeId == leaveTypeId.Value);
        var raw = await q.OrderBy(r => r.LeaveTypeId).ThenBy(r => r.EventCaseCode).ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveTypeLegalRuleReadDto>>.Ok(raw.Select(MapLegalRule).ToList());
    }

    public async Task<ServiceResult<LeaveTypeLegalRuleReadDto>> GetLegalRuleByIdAsync(int id, CancellationToken ct = default)
    {
        var r = await _db.LeaveTypeLegalRules.FindAsync(new object[] { id }, ct);
        return r == null ? ServiceResult<LeaveTypeLegalRuleReadDto>.Fail("Règle légale introuvable.") : ServiceResult<LeaveTypeLegalRuleReadDto>.Ok(MapLegalRule(r));
    }

    public async Task<ServiceResult<LeaveTypeLegalRuleReadDto>> CreateLegalRuleAsync(LeaveTypeLegalRuleCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        // Vérifier que le LeaveType existe
        var leaveType = await _db.LeaveTypes.FirstOrDefaultAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null, ct);
        if (leaveType == null)
            return ServiceResult<LeaveTypeLegalRuleReadDto>.Fail("Type de congé non trouvé.");

        // Unicité EventCaseCode par LeaveTypeId
        var ruleExists = await _db.LeaveTypeLegalRules.AnyAsync(r => r.LeaveTypeId == dto.LeaveTypeId && r.EventCaseCode == dto.EventCaseCode, ct);
        if (ruleExists)
            return ServiceResult<LeaveTypeLegalRuleReadDto>.Fail($"Une règle légale avec le code '{dto.EventCaseCode}' existe déjà pour ce type de congé.");

        if (dto.DaysGranted <= 0)
            return ServiceResult<LeaveTypeLegalRuleReadDto>.Fail("DaysGranted doit être supérieur à 0.");

        var r = new LeaveTypeLegalRule
        {
            LeaveTypeId = dto.LeaveTypeId,
            EventCaseCode = dto.EventCaseCode.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            DaysGranted = dto.DaysGranted,
            LegalArticle = dto.LegalArticle?.Trim() ?? string.Empty,
            CanBeDiscountinuous = dto.CanBeDiscontinuous,
            MustBeUsedWithinDays = dto.MustBeUsedWithinDays,
            CreatedBy = createdBy
        };
        _db.LeaveTypeLegalRules.Add(r);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveTypeLegalRuleReadDto>.Ok(MapLegalRule(r));
    }

    public async Task<ServiceResult<LeaveTypeLegalRuleReadDto>> PatchLegalRuleAsync(int id, LeaveTypeLegalRulePatchDto dto, int updatedBy, CancellationToken ct = default)
    {
        var r = await _db.LeaveTypeLegalRules
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r == null) return ServiceResult<LeaveTypeLegalRuleReadDto>.Fail("Règle légale introuvable.");

        // Unicité EventCaseCode si changement
        if (dto.EventCaseCode != null && dto.EventCaseCode != r.EventCaseCode)
        {
            var codeExists = await _db.LeaveTypeLegalRules.AnyAsync(x => x.Id != id && x.LeaveTypeId == r.LeaveTypeId && x.EventCaseCode == dto.EventCaseCode, ct);
            if (codeExists)
                return ServiceResult<LeaveTypeLegalRuleReadDto>.Fail($"Une règle légale avec le code '{dto.EventCaseCode}' existe déjà pour ce type de congé.");
            r.EventCaseCode = dto.EventCaseCode.Trim();
        }

        if (dto.Description != null) r.Description = dto.Description.Trim();
        if (dto.DaysGranted != null)
        {
            if (dto.DaysGranted <= 0)
                return ServiceResult<LeaveTypeLegalRuleReadDto>.Fail("DaysGranted doit être supérieur à 0.");
            r.DaysGranted = dto.DaysGranted.Value;
        }
        if (dto.LegalArticle != null) r.LegalArticle = dto.LegalArticle.Trim();
        if (dto.CanBeDiscontinuous != null) r.CanBeDiscountinuous = dto.CanBeDiscontinuous.Value;
        if (dto.MustBeUsedWithinDays != null) r.MustBeUsedWithinDays = dto.MustBeUsedWithinDays;

        r.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveTypeLegalRuleReadDto>.Ok(MapLegalRule(r));
    }

    public async Task<ServiceResult> DeleteLegalRuleAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var r = await _db.LeaveTypeLegalRules
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r == null) return ServiceResult.Fail("Règle légale introuvable.");

        // Vérification dépendances
        var hasRequests = await _db.LeaveRequests.AnyAsync(lr => lr.LegalRuleId == id && lr.DeletedAt == null, ct);
        if (hasRequests)
            return ServiceResult.Fail("Impossible de supprimer cette règle car elle est utilisée dans des demandes de congés.");

        // Soft delete (cohérence avec le reste du service)
        r.DeletedAt = DateTimeOffset.UtcNow;
        r.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── LeaveRequest ──────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<LeaveRequestReadDto>>> GetLeaveRequestsAsync(
        int? companyId, int? employeeId, LeaveRequestStatus? status, CancellationToken ct = default)
    {
        var q = _db.LeaveRequests.AsNoTracking()
            .Where(lr => lr.DeletedAt == null)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.LegalRule)
            .AsQueryable();
        if (companyId.HasValue) q = q.Where(lr => lr.CompanyId == companyId.Value);
        if (employeeId.HasValue) q = q.Where(lr => lr.EmployeeId == employeeId.Value);
        if (status.HasValue) q = q.Where(lr => lr.Status == status.Value);
        var raw = await q.OrderByDescending(lr => lr.RequestedAt).ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveRequestReadDto>>.Ok(raw.Select(MapLeaveRequest).ToList());
    }

    public async Task<ServiceResult<IEnumerable<LeaveRequestReadDto>>> GetLeaveRequestsByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        var raw = await _db.LeaveRequests.AsNoTracking()
            .Where(lr => lr.DeletedAt == null && lr.EmployeeId == employeeId)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.Policy)
            .Include(lr => lr.LegalRule)
            .OrderByDescending(lr => lr.RequestedAt)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveRequestReadDto>>.Ok(raw.Select(MapLeaveRequest).ToList());
    }

    public async Task<ServiceResult<LeaveRequestReadDto>> GetLeaveRequestByIdAsync(int id, CancellationToken ct = default)
    {
        var lr = await _db.LeaveRequests.AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Include(x => x.LeaveType)
            .Include(x => x.LegalRule)
            .Include(x => x.Policy)
            .FirstOrDefaultAsync(ct);
        return lr == null
            ? ServiceResult<LeaveRequestReadDto>.Fail("Demande introuvable.")
            : ServiceResult<LeaveRequestReadDto>.Ok(MapLeaveRequest(lr));
    }

    public async Task<ServiceResult<IEnumerable<LeaveRequestReadDto>>> GetPendingApprovalAsync(int? companyId, CancellationToken ct = default)
    {
        var q = _db.LeaveRequests.AsNoTracking()
            .Where(lr => lr.DeletedAt == null && lr.Status == LeaveRequestStatus.Submitted)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.LegalRule)
            .Include(lr => lr.Employee)
            .AsQueryable();
        if (companyId.HasValue) q = q.Where(lr => lr.CompanyId == companyId.Value);
        var raw = await q.OrderBy(lr => lr.SubmittedAt).ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveRequestReadDto>>.Ok(raw.Select(MapLeaveRequest).ToList());
    }

    public async Task<ServiceResult<LeaveRequestReadDto>> CreateLeaveRequestForSelfAsync(LeaveRequestCreateDto dto, int userId, CancellationToken ct = default)
    {
        var user = await LoadUserWithRolesAsync(userId, ct);
        bool isRhOrAdmin = UserIsRhOrAdmin(user);
        var employee = await ResolveEmployeeForSelfAsync(userId, ct);
        if (employee == null)
            return ServiceResult<LeaveRequestReadDto>.Fail("Employé introuvable.");

        if (dto.StartDate >= dto.EndDate)
            return ServiceResult<LeaveRequestReadDto>.Fail("La date de début doit être antérieure à la date de fin.");

        var leaveType = await _db.LeaveTypes.AsNoTracking()
            .FirstOrDefaultAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null, ct);
        if (leaveType == null)
            return ServiceResult<LeaveRequestReadDto>.Fail("Type de congé introuvable.");

        var policy = await ResolveCreatePolicyAsync(employee.CompanyId, dto, ct);
        if (policy == null)
            return ServiceResult<LeaveRequestReadDto>.Fail("Aucune politique de congé définie pour ce type de congé dans l'entreprise.");

        var eligErr = ValidateEligibility6MonthsIfNeeded(employee, policy);
        if (eligErr != null)
            return ServiceResult<LeaveRequestReadDto>.Fail(eligErr);

        return await CreateLeaveRequestCoreAsync(employee, dto, policy, userId, isRhOrAdmin, ct);
    }

    public async Task<ServiceResult> CreateLeaveRequestForOtherEmployeeAsync(int targetEmployeeId, LeaveRequestCreateDto dto, int actorUserId, CancellationToken ct = default)
    {
        var actor = await LoadUserWithRolesAsync(actorUserId, ct);
        if (actor == null) return ServiceResult.Fail("Utilisateur introuvable.");

        bool isRhOrAdmin = UserIsRhOrAdmin(actor);

        var target = await _db.Employees
            .Include(e => e.Contracts)
            .FirstOrDefaultAsync(e => e.Id == targetEmployeeId && e.DeletedAt == null, ct);
        if (target == null) return ServiceResult.Fail("Employé introuvable.");

        if (!isRhOrAdmin)
        {
            var isManager = await _db.Employees.AsNoTracking()
                .AnyAsync(e => e.Id == targetEmployeeId && e.ManagerId == actorUserId, ct);
            if (!isManager)
                return ServiceResult.Fail("Vous devez être RH, Admin ou Manager pour créer un congé pour cet utilisateur.");
        }

        if (dto.StartDate >= dto.EndDate)
            return ServiceResult.Fail("La date de début doit être antérieure à la date de fin.");

        var leaveTypeOk = await _db.LeaveTypes.AsNoTracking()
            .AnyAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null, ct);
        if (!leaveTypeOk) return ServiceResult.Fail("Type de congé non trouvé.");

        var policy = await ResolveCreatePolicyAsync(target.CompanyId, dto, ct);
        if (policy == null)
            return ServiceResult.Fail("Aucune politique de congé définie pour ce type de congé dans l'entreprise.");

        var eligErr = ValidateEligibility6MonthsIfNeeded(target, policy);
        if (eligErr != null) return ServiceResult.Fail(eligErr);

        var core = await CreateLeaveRequestCoreAsync(target, dto, policy, actorUserId, isRhOrAdmin, ct);
        return core.Success ? ServiceResult.Ok() : ServiceResult.Fail(core.Error ?? "Erreur création demande.");
    }

    public async Task<ServiceResult<LeaveRequestReadDto>> CreateLeaveRequestAsync(int employeeId, LeaveRequestCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var employee = await _db.Employees
            .Include(e => e.Contracts)
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.DeletedAt == null, ct);
        if (employee == null)
            return ServiceResult<LeaveRequestReadDto>.Fail("Employé introuvable.");

        var policy = await ResolveCreatePolicyAsync(employee.CompanyId, dto, ct);
        if (policy == null)
            return ServiceResult<LeaveRequestReadDto>.Fail("Aucune politique de congé définie pour ce type de congé dans l'entreprise.");

        var eligErr = ValidateEligibility6MonthsIfNeeded(employee, policy);
        if (eligErr != null) return ServiceResult<LeaveRequestReadDto>.Fail(eligErr);

        return await CreateLeaveRequestCoreAsync(employee, dto, policy, createdBy, isRhOrAdmin: false, ct);
    }

    public Task<ServiceResult<LeaveRequestReadDto>> PatchLeaveRequestAsync(int id, LeaveRequestPatchDto dto, int updatedBy, CancellationToken ct = default)
        => UpdateLeaveRequestDraftAsync(id, dto, updatedBy, ct);

    public Task<ServiceResult<LeaveRequestReadDto>> PutLeaveRequestAsync(int id, LeaveRequestPatchDto dto, int updatedBy, CancellationToken ct = default)
        => UpdateLeaveRequestDraftAsync(id, dto, updatedBy, ct);

    public async Task<ServiceResult<LeaveRequestReadDto>> SubmitLeaveRequestAsync(int id, int userId, CancellationToken ct = default)
    {
        var request = await _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.LegalRule)
            .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null, ct);

        if (request == null) return ServiceResult<LeaveRequestReadDto>.Fail("Demande introuvable.");
        if (request.Status != LeaveRequestStatus.Draft)
            return ServiceResult<LeaveRequestReadDto>.Fail("Seules les demandes en brouillon peuvent être soumises.");
        if (request.StartDate > request.EndDate)
            return ServiceResult<LeaveRequestReadDto>.Fail("Période invalide (StartDate > EndDate).");

        if (!request.LegalRuleId.HasValue)
        {
            var policy = await ResolvePolicyForBalanceCheckAsync(request.CompanyId, request.LeaveTypeId, request.StartDate, ct);
            if (policy == null)
                return ServiceResult<LeaveRequestReadDto>.Fail("Aucune politique active trouvée pour ce type de congé.");
            if (policy.RequiresBalance && !policy.AllowNegativeBalance)
            {
                var total = await GetTotalNonExpiredClosingDaysAsync(request.CompanyId, request.EmployeeId, request.LeaveTypeId, request.StartDate, ct);
                if (total < request.WorkingDaysDeducted)
                    return ServiceResult<LeaveRequestReadDto>.Fail("Solde de congés insuffisant.");
            }
        }

        var hasOverlap = await _db.LeaveRequests.AnyAsync(lr =>
            lr.Id != id &&
            lr.EmployeeId == request.EmployeeId &&
            lr.DeletedAt == null &&
            (lr.Status == LeaveRequestStatus.Submitted || lr.Status == LeaveRequestStatus.Approved) &&
            lr.StartDate <= request.EndDate && lr.EndDate >= request.StartDate, ct);
        if (hasOverlap)
            return ServiceResult<LeaveRequestReadDto>.Fail("Cette période chevauche une demande déjà soumise ou approuvée.");

        request.Status = LeaveRequestStatus.Submitted;
        request.SubmittedAt = DateTimeOffset.UtcNow;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedBy = userId;

        _db.LeaveRequestApprovalHistories.Add(new LeaveRequestApprovalHistory
        {
            LeaveRequestId = request.Id,
            Action = LeaveApprovalAction.Submitted,
            ActionAt = DateTimeOffset.UtcNow,
            ActionBy = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId
        });

        await _db.SaveChangesAsync(ct);

        await _leaveEventLog.LogLeaveRequestEventAsync(
            request.CompanyId, request.EmployeeId, request.Id,
            LeaveLog.RequestSubmitted, "Draft", "Submitted", userId, ct);

        var reloaded = await ReloadLeaveRequestReadAsync(request.Id, ct);
        return ServiceResult<LeaveRequestReadDto>.Ok(MapLeaveRequest(reloaded!));
    }

    public async Task<ServiceResult<LeaveRequestReadDto>> ApproveLeaveRequestAsync(int id, string? comment, int decidedBy, CancellationToken ct = default)
    {
        var request = await _db.LeaveRequests
            .Include(lr => lr.Policy)
            .Include(lr => lr.LegalRule)
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null, ct);

        if (request == null) return ServiceResult<LeaveRequestReadDto>.Fail("Demande introuvable.");
        if (request.Status != LeaveRequestStatus.Submitted)
            return ServiceResult<LeaveRequestReadDto>.Fail("Seules les demandes soumises peuvent être approuvées.");

        request.Status = LeaveRequestStatus.Approved;
        request.DecisionAt = DateTimeOffset.UtcNow;
        request.DecisionBy = decidedBy;
        request.DecisionComment = comment;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedBy = decidedBy;

        _db.LeaveRequestApprovalHistories.Add(new LeaveRequestApprovalHistory
        {
            LeaveRequestId = request.Id,
            Action = LeaveApprovalAction.Approved,
            ActionAt = DateTimeOffset.UtcNow,
            ActionBy = decidedBy,
            Comment = comment,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = decidedBy
        });

        await _db.SaveChangesAsync(ct);

        if (request.Policy != null && request.Policy.RequiresBalance && !request.LegalRuleId.HasValue)
            await TryRecalculateLeaveBalanceMonthAsync(
                request.CompanyId, request.EmployeeId, request.LeaveTypeId, request.StartDate, decidedBy, ct);

        await _leaveEventLog.LogLeaveRequestEventAsync(
            request.CompanyId, request.EmployeeId, request.Id,
            LeaveLog.RequestApproved, "Submitted", "Approved", decidedBy, ct);

        var reloaded = await ReloadLeaveRequestReadAsync(request.Id, ct);
        return ServiceResult<LeaveRequestReadDto>.Ok(MapLeaveRequest(reloaded!));
    }

    public async Task<ServiceResult<LeaveRequestReadDto>> RejectLeaveRequestAsync(int id, string? comment, int decidedBy, CancellationToken ct = default)
    {
        var request = await _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.LegalRule)
            .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null, ct);

        if (request == null) return ServiceResult<LeaveRequestReadDto>.Fail("Demande introuvable.");
        if (request.Status != LeaveRequestStatus.Submitted)
            return ServiceResult<LeaveRequestReadDto>.Fail("Seules les demandes soumises peuvent être rejetées.");

        request.Status = LeaveRequestStatus.Rejected;
        request.DecisionAt = DateTimeOffset.UtcNow;
        request.DecisionBy = decidedBy;
        request.DecisionComment = comment?.Trim();
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedBy = decidedBy;

        _db.LeaveRequestApprovalHistories.Add(new LeaveRequestApprovalHistory
        {
            LeaveRequestId = request.Id,
            Action = LeaveApprovalAction.Rejected,
            ActionAt = DateTimeOffset.UtcNow,
            ActionBy = decidedBy,
            Comment = comment,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = decidedBy
        });

        await _db.SaveChangesAsync(ct);

        await _leaveEventLog.LogLeaveRequestEventAsync(
            request.CompanyId, request.EmployeeId, request.Id,
            LeaveLog.RequestRejected, "Submitted", "Rejected", decidedBy, ct);

        var reloaded = await ReloadLeaveRequestReadAsync(request.Id, ct);
        return ServiceResult<LeaveRequestReadDto>.Ok(MapLeaveRequest(reloaded!));
    }

    public async Task<ServiceResult<LeaveRequestReadDto>> CancelLeaveRequestAsync(int id, string? comment, int userId, CancellationToken ct = default)
    {
        var request = await _db.LeaveRequests
            .Include(lr => lr.Policy)
            .Include(lr => lr.LegalRule)
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null, ct);

        if (request == null) return ServiceResult<LeaveRequestReadDto>.Fail("Demande introuvable.");
        if (request.Status != LeaveRequestStatus.Approved && request.Status != LeaveRequestStatus.Submitted)
            return ServiceResult<LeaveRequestReadDto>.Fail("Seules les demandes approuvées ou soumises peuvent être annulées.");

        request.Status = LeaveRequestStatus.Cancelled;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedBy = userId;

        _db.LeaveRequestApprovalHistories.Add(new LeaveRequestApprovalHistory
        {
            LeaveRequestId = request.Id,
            Action = LeaveApprovalAction.Cancelled,
            ActionAt = DateTimeOffset.UtcNow,
            ActionBy = userId,
            Comment = comment,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId
        });

        await _db.SaveChangesAsync(ct);

        if (request.Policy != null && request.Policy.RequiresBalance && !request.LegalRuleId.HasValue)
            await TryRecalculateLeaveBalanceMonthAsync(
                request.CompanyId, request.EmployeeId, request.LeaveTypeId, request.StartDate, userId, ct);

        await _leaveEventLog.LogLeaveRequestEventAsync(
            request.CompanyId, request.EmployeeId, request.Id,
            LeaveLog.RequestCancelled, "Approved/Submitted", "Cancelled", userId, ct);

        var reloaded = await ReloadLeaveRequestReadAsync(request.Id, ct);
        return ServiceResult<LeaveRequestReadDto>.Ok(MapLeaveRequest(reloaded!));
    }

    public async Task<ServiceResult<LeaveRequestReadDto>> RenounceLeaveRequestAsync(int id, int userId, CancellationToken ct = default)
    {
        var request = await _db.LeaveRequests
            .Include(lr => lr.Policy)
            .Include(lr => lr.LegalRule)
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null, ct);

        if (request == null) return ServiceResult<LeaveRequestReadDto>.Fail("Demande introuvable.");
        if (request.Status != LeaveRequestStatus.Approved)
            return ServiceResult<LeaveRequestReadDto>.Fail("Seules les demandes approuvées peuvent faire l'objet d'une renonciation.");

        request.Status = LeaveRequestStatus.Renounced;
        request.IsRenounced = true;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        if (request.Policy != null && request.Policy.RequiresBalance && !request.LegalRuleId.HasValue)
            await TryRecalculateLeaveBalanceMonthAsync(
                request.CompanyId, request.EmployeeId, request.LeaveTypeId, request.StartDate, userId, ct);

        await _leaveEventLog.LogLeaveRequestEventAsync(
            request.CompanyId, request.EmployeeId, request.Id,
            LeaveLog.RequestRenounced, "Approved", "Renounced", userId, ct);

        var reloaded = await ReloadLeaveRequestReadAsync(request.Id, ct);
        return ServiceResult<LeaveRequestReadDto>.Ok(MapLeaveRequest(reloaded!));
    }

    public async Task<ServiceResult> DeleteLeaveRequestAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var request = await _db.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null, ct);
        if (request == null) return ServiceResult.Fail("Demande introuvable.");
        if (request.Status != LeaveRequestStatus.Draft)
            return ServiceResult.Fail("Seules les demandes en brouillon peuvent être supprimées.");

        request.DeletedAt = DateTimeOffset.UtcNow;
        request.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);

        await _leaveEventLog.LogLeaveRequestEventAsync(
            request.CompanyId, request.EmployeeId, request.Id,
            LeaveLog.RequestDeleted, $"Demande ID {request.Id} supprimée", null, deletedBy, ct);

        return ServiceResult.Ok();
    }

    // ── Helpers leave request ─────────────────────────────────────────────────

    private async Task<Users?> LoadUserWithRolesAsync(int userId, CancellationToken ct)
        => await _db.Users.AsNoTracking()
            .Include(u => u.UsersRoles!)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

    private static bool UserIsRhOrAdmin(Users? user) =>
        user?.UsersRoles?.Any(ur =>
            ur.Role.Name.Equals("RH", StringComparison.OrdinalIgnoreCase) ||
            ur.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase)) == true;

    private async Task<Payzen.Domain.Entities.Employee.Employee?> ResolveEmployeeForSelfAsync(int userId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user?.EmployeeId is int empId)
        {
            var byLink = await _db.Employees
                .Include(e => e.Contracts)
                .FirstOrDefaultAsync(e => e.Id == empId && e.DeletedAt == null, ct);
            if (byLink != null) return byLink;
        }
        return await _db.Employees
            .Include(e => e.Contracts)
            .FirstOrDefaultAsync(e => e.Id == userId && e.DeletedAt == null, ct);
    }

    private async Task<LeaveTypePolicy?> ResolveCreatePolicyAsync(int companyId, LeaveRequestCreateDto dto, CancellationToken ct)
    {
        int? explicitId = dto.LeaveTypePolicyId;
        if (explicitId is > 0)
            return await _db.LeaveTypePolicies.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == explicitId && p.DeletedAt == null
                    && p.LeaveTypeId == dto.LeaveTypeId
                    && (p.CompanyId == null || p.CompanyId == companyId), ct);
        return await ResolvePolicyForBalanceCheckAsync(companyId, dto.LeaveTypeId, dto.StartDate, ct);
    }

    private async Task<LeaveTypePolicy?> ResolvePolicyForBalanceCheckAsync(int companyId, int leaveTypeId, DateOnly asOfDate, CancellationToken ct)
    {
        var companyPolicy = await _db.LeaveTypePolicies.AsNoTracking()
            .Where(p => p.DeletedAt == null && p.IsEnabled && p.LeaveTypeId == leaveTypeId && p.CompanyId == companyId)
            .Where(p => p.EffectiveFrom == null || p.EffectiveFrom <= asOfDate)
            .Where(p => p.EffectiveTo == null || p.EffectiveTo >= asOfDate)
            .OrderByDescending(p => p.EffectiveFrom ?? new DateOnly(1900, 1, 1))
            .FirstOrDefaultAsync(ct);
        if (companyPolicy != null) return companyPolicy;

        return await _db.LeaveTypePolicies.AsNoTracking()
            .Where(p => p.DeletedAt == null && p.IsEnabled && p.LeaveTypeId == leaveTypeId && p.CompanyId == null)
            .Where(p => p.EffectiveFrom == null || p.EffectiveFrom <= asOfDate)
            .Where(p => p.EffectiveTo == null || p.EffectiveTo >= asOfDate)
            .OrderByDescending(p => p.EffectiveFrom ?? new DateOnly(1900, 1, 1))
            .FirstOrDefaultAsync(ct);
    }

    private static string? ValidateEligibility6MonthsIfNeeded(
        Payzen.Domain.Entities.Employee.Employee employee, LeaveTypePolicy policy)
    {
        if (!policy.RequiresEligibility6Months) return null;
        var firstContract = employee.Contracts.OrderBy(c => c.StartDate).FirstOrDefault(c => c.DeletedAt == null);
        if (firstContract == null) return "Aucun contrat actif trouvé pour l'employé.";
        var employmentDate = DateOnly.FromDateTime(firstContract.StartDate);
        if (DateOnly.FromDateTime(DateTime.UtcNow) < employmentDate.AddMonths(6))
            return "L'employé doit avoir au moins 6 mois d'ancienneté pour ce type de congé.";
        return null;
    }

    private async Task<ServiceResult<LeaveRequestReadDto>> CreateLeaveRequestCoreAsync(
        Payzen.Domain.Entities.Employee.Employee employee,
        LeaveRequestCreateDto dto,
        LeaveTypePolicy leavePolicy,
        int userId,
        bool isRhOrAdmin,
        CancellationToken ct)
    {
        var calendarDays = dto.EndDate.DayNumber - dto.StartDate.DayNumber + 1;
        var workingDays = await _workingDays.CalculateWorkingDaysAsync(employee.CompanyId, dto.StartDate, dto.EndDate, ct);

        var request = new LeaveRequest
        {
            EmployeeId = employee.Id,
            CompanyId = employee.CompanyId,
            LeaveTypeId = dto.LeaveTypeId,
            PolicyId = leavePolicy.Id,
            LegalRuleId = dto.LegalRuleId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = isRhOrAdmin ? LeaveRequestStatus.Approved : LeaveRequestStatus.Draft,
            SubmittedAt = isRhOrAdmin ? DateTimeOffset.UtcNow : null,
            DecisionAt = isRhOrAdmin ? DateTimeOffset.UtcNow : null,
            DecisionBy = isRhOrAdmin ? userId : null,
            DecisionComment = isRhOrAdmin ? "Approbation automatique (RH/Admin)" : null,
            RequestedAt = DateTimeOffset.UtcNow,
            CalendarDays = calendarDays,
            WorkingDaysDeducted = workingDays,
            EmployeeNote = dto.EmployeeNote?.Trim(),
            CreatedBy = userId
        };

        _db.LeaveRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        if (isRhOrAdmin)
        {
            _db.LeaveRequestApprovalHistories.Add(new LeaveRequestApprovalHistory
            {
                LeaveRequestId = request.Id,
                Action = LeaveApprovalAction.Approved,
                ActionAt = DateTimeOffset.UtcNow,
                ActionBy = userId,
                Comment = "Approbation automatique (RH/Admin)",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            });

            await _db.SaveChangesAsync(ct);

            if (leavePolicy.RequiresBalance && !dto.LegalRuleId.HasValue)
                await TryRecalculateLeaveBalanceMonthAsync(
                    request.CompanyId, request.EmployeeId, request.LeaveTypeId, request.StartDate, userId, ct);
        }

        await _leaveEventLog.LogLeaveRequestEventAsync(
            employee.CompanyId, employee.Id, request.Id,
            LeaveLog.RequestCreated, null, $"Demande créée: {dto.StartDate} - {dto.EndDate}", userId, ct);

        var reloaded = await ReloadLeaveRequestReadAsync(request.Id, ct);
        return ServiceResult<LeaveRequestReadDto>.Ok(MapLeaveRequest(reloaded!));
    }

    private async Task<ServiceResult<LeaveRequestReadDto>> UpdateLeaveRequestDraftAsync(int id, LeaveRequestPatchDto dto, int userId, CancellationToken ct)
    {
        var request = await _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.LegalRule)
            .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null, ct);

        if (request == null) return ServiceResult<LeaveRequestReadDto>.Fail("Demande introuvable.");
        if (request.Status != LeaveRequestStatus.Draft)
            return ServiceResult<LeaveRequestReadDto>.Fail("Seules les demandes en brouillon peuvent être modifiées.");

        if (dto.LeaveTypeId.HasValue && dto.LeaveTypeId.Value != request.LeaveTypeId)
        {
            var ltOk = await _db.LeaveTypes.AnyAsync(x => x.Id == dto.LeaveTypeId.Value && x.DeletedAt == null, ct);
            if (!ltOk) return ServiceResult<LeaveRequestReadDto>.Fail("Type de congé introuvable.");
            request.LeaveTypeId = dto.LeaveTypeId.Value;
            var createDto = new LeaveRequestCreateDto
            {
                LeaveTypeId = request.LeaveTypeId,
                LeaveTypePolicyId = dto.LeaveTypePolicyId,
                LegalRuleId = dto.LegalRuleId,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };
            var pol = await ResolveCreatePolicyAsync(request.CompanyId, createDto, ct);
            if (pol == null) return ServiceResult<LeaveRequestReadDto>.Fail("Aucune politique de congé définie pour ce type de congé dans l'entreprise.");
            request.PolicyId = pol.Id;
        }

        if (dto.LegalRuleId.HasValue) request.LegalRuleId = dto.LegalRuleId;
        if (dto.StartDate.HasValue) request.StartDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue) request.EndDate = dto.EndDate.Value;

        if (request.StartDate >= request.EndDate)
            return ServiceResult<LeaveRequestReadDto>.Fail("La date de début doit être antérieure à la date de fin.");

        request.CalendarDays = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        request.WorkingDaysDeducted = await _workingDays.CalculateWorkingDaysAsync(request.CompanyId, request.StartDate, request.EndDate, ct);

        if (dto.EmployeeNote != null) request.EmployeeNote = dto.EmployeeNote.Trim();
        if (dto.ManagerNote != null) request.ManagerNote = dto.ManagerNote;

        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedBy = userId;
        await _db.SaveChangesAsync(ct);

        await _leaveEventLog.LogLeaveRequestEventAsync(
            request.CompanyId, request.EmployeeId, request.Id,
            LeaveLog.RequestUpdated, null, "Mise à jour demande", userId, ct);

        var reloaded = await ReloadLeaveRequestReadAsync(request.Id, ct);
        return ServiceResult<LeaveRequestReadDto>.Ok(MapLeaveRequest(reloaded!));
    }

    /// <summary>
    /// Recalcule acquisition, consommation (somme des demandes approuvées du mois), report et closing.
    /// Évite les soldes avec Accrued nul et Used positif après approbation (closing très négatif).
    /// </summary>
    private async Task TryRecalculateLeaveBalanceMonthAsync(
        int companyId, int employeeId, int leaveTypeId, DateOnly leaveStartDate, int actorUserId, CancellationToken ct)
    {
        var y = leaveStartDate.Year;
        var m = leaveStartDate.Month;
        await _leaveBalanceRecalc.RecalculateRangeThroughMonthAsync(
            companyId, employeeId, leaveTypeId, y, m, actorUserId, ct);
    }

    private async Task<LeaveBalance?> GetOrCreateBalanceForMonthAsync(
        int companyId, int employeeId, int leaveTypeId, int year, int month, int userId, CancellationToken ct)
    {
        var balance = await _db.LeaveBalances.FirstOrDefaultAsync(b =>
            b.CompanyId == companyId && b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId
            && b.Year == year && b.Month == month && b.DeletedAt == null, ct);
        if (balance != null) return balance;

        var now = DateTimeOffset.UtcNow;
        var nb = new LeaveBalance
        {
            CompanyId = companyId, EmployeeId = employeeId, LeaveTypeId = leaveTypeId,
            Year = year, Month = month,
            OpeningDays = 0, AccruedDays = 0, UsedDays = 0,
            CarryInDays = 0, CarryOutDays = 0, ClosingDays = 0,
            LastRecalculatedAt = now, CreatedAt = now, CreatedBy = userId
        };
        nb.CarryoverExpiresOn = nb.GetBalanceExpiresOn();
        _db.LeaveBalances.Add(nb);
        await _db.SaveChangesAsync(ct);
        return nb;
    }

    private void VersionLeaveBalance(LeaveBalance currentBalance, decimal usedDaysDelta, int userId)
    {
        currentBalance.DeletedAt = DateTimeOffset.UtcNow;
        currentBalance.DeletedBy = userId;
        var now = DateTimeOffset.UtcNow;
        var newUsed = currentBalance.UsedDays + usedDaysDelta;
        var newClosing = currentBalance.OpeningDays + currentBalance.AccruedDays + currentBalance.CarryInDays
                         - newUsed - currentBalance.CarryOutDays;
        _db.LeaveBalances.Add(new LeaveBalance
        {
            CompanyId = currentBalance.CompanyId,
            EmployeeId = currentBalance.EmployeeId,
            LeaveTypeId = currentBalance.LeaveTypeId,
            Year = currentBalance.Year,
            Month = currentBalance.Month,
            OpeningDays = currentBalance.OpeningDays,
            AccruedDays = currentBalance.AccruedDays,
            UsedDays = newUsed,
            CarryInDays = currentBalance.CarryInDays,
            CarryOutDays = currentBalance.CarryOutDays,
            ClosingDays = newClosing,
            CarryoverExpiresOn = currentBalance.CarryoverExpiresOn,
            LastRecalculatedAt = currentBalance.LastRecalculatedAt,
            CreatedAt = now, CreatedBy = userId
        });
    }

    private async Task<decimal> GetTotalNonExpiredClosingDaysAsync(
        int companyId, int employeeId, int leaveTypeId, DateOnly asOfDate, CancellationToken ct)
    {
        var list = await _db.LeaveBalances.AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId && b.DeletedAt == null)
            .Where(b => b.CarryoverExpiresOn != null && b.CarryoverExpiresOn.Value >= asOfDate)
            .ToListAsync(ct);
        return list.Sum(b => b.ClosingDays);
    }

    private async Task<LeaveRequest?> ReloadLeaveRequestReadAsync(int id, CancellationToken ct)
        => await _db.LeaveRequests.AsNoTracking()
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.LegalRule)
            .Include(lr => lr.Policy)
            .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null, ct);

    // ── LeaveBalance ──────────────────────────────────────────────────────────

    public Task<ServiceResult<IEnumerable<LeaveBalanceReadDto>>> GetBalancesAsync(int employeeId, int? leaveTypeId, CancellationToken ct = default)
        => GetBalancesFilteredAsync(null, employeeId, null, null, leaveTypeId, ct);

    public async Task<ServiceResult<IEnumerable<LeaveBalanceReadDto>>> GetBalancesFilteredAsync(
        int? companyId, int? employeeId, int? year, int? month, int? leaveTypeId, CancellationToken ct = default)
    {
        var q = _db.LeaveBalances.AsNoTracking().Where(lb => lb.DeletedAt == null).AsQueryable();
        if (companyId.HasValue) q = q.Where(lb => lb.CompanyId == companyId.Value);
        if (employeeId.HasValue) q = q.Where(lb => lb.EmployeeId == employeeId.Value);
        if (year.HasValue) q = q.Where(lb => lb.Year == year.Value);
        if (month.HasValue) q = q.Where(lb => lb.Month == month.Value);
        if (leaveTypeId.HasValue) q = q.Where(lb => lb.LeaveTypeId == leaveTypeId.Value);

        var raw = await q
            .OrderBy(b => b.Year)
            .ThenBy(b => b.Month)
            .ThenBy(b => b.EmployeeId)
            .ThenBy(b => b.LeaveTypeId)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveBalanceReadDto>>.Ok(raw.Select(lb => MapBalance(lb)).ToList());
    }

    public async Task<ServiceResult<LeaveBalanceReadDto>> GetBalanceByIdAsync(int id, CancellationToken ct = default)
    {
        var lb = await _db.LeaveBalances.FindAsync(new object[] { id }, ct);
        if (lb == null || lb.DeletedAt != null) return ServiceResult<LeaveBalanceReadDto>.Fail("Solde introuvable.");
        return ServiceResult<LeaveBalanceReadDto>.Ok(MapBalance(lb));
    }

    public async Task<ServiceResult<IEnumerable<LeaveBalanceReadDto>>> GetBalancesByYearAsync(int employeeId, int year, CancellationToken ct = default)
    {
        var raw = await _db.LeaveBalances
            .AsNoTracking()
            .Where(lb => lb.EmployeeId == employeeId && lb.Year == year && lb.DeletedAt == null)
            .OrderBy(lb => lb.Month).ThenBy(lb => lb.LeaveTypeId)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveBalanceReadDto>>.Ok(raw.Select(lb => MapBalance(lb)).ToList());
    }

    public async Task<ServiceResult<IEnumerable<LeaveBalanceReadDto>>> GetBalancesByYearMonthAsync(int employeeId, int year, int month, CancellationToken ct = default)
    {
        if (month < 1 || month > 12) return ServiceResult<IEnumerable<LeaveBalanceReadDto>>.Fail("Le mois doit être entre 1 et 12.");
        var raw = await _db.LeaveBalances
            .AsNoTracking()
            .Where(lb => lb.EmployeeId == employeeId && lb.Year == year && lb.Month == month && lb.DeletedAt == null)
            .OrderBy(lb => lb.LeaveTypeId)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveBalanceReadDto>>.Ok(raw.Select(lb => MapBalance(lb)).ToList());
    }

    public async Task<ServiceResult<object>> GetBalanceSummaryAsync(int employeeId, int? companyId, CancellationToken ct = default)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var q = _db.LeaveBalances
            .Where(lb => lb.EmployeeId == employeeId && lb.DeletedAt == null);
        if (companyId.HasValue)
            q = q.Where(lb => lb.CompanyId == companyId.Value);

        var balances = await q.Include(lb => lb.LeaveType).ToListAsync(ct);

        var byLeaveType = balances
            .Where(b => b.CarryoverExpiresOn != null && b.CarryoverExpiresOn.Value >= asOf)
            .GroupBy(b => new { b.LeaveTypeId, b.LeaveType })
            .Select(g => new
            {
                LeaveTypeId = g.Key.LeaveTypeId,
                LeaveTypeName = g.Key.LeaveType?.LeaveNameFr,
                LeaveTypeCode = g.Key.LeaveType?.LeaveCode,
                TotalAvailable = g.Sum(x => x.ClosingDays),
                ByMonth = g.Select(x => new
                {
                    x.Year,
                    x.Month,
                    x.ClosingDays,
                    BalanceExpiresOn = x.GetBalanceExpiresOn()
                }).OrderBy(x => x.Year).ThenBy(x => x.Month).ToList()
            }).ToList();

        return ServiceResult<object>.Ok(new
        {
            EmployeeId = employeeId,
            AsOf = asOf,
            TotalAvailableByLeaveType = byLeaveType,
            AllBalances = balances.OrderBy(b => b.Year).ThenBy(b => b.Month).Select(b => MapBalance(b, asOf)).ToList()
        });
    }

    public async Task<ServiceResult<LeaveBalanceRecalculateResultDto>> RecalculateBalancesForMonthAsync(
        int employeeId, int year, int month, int? companyId, int? leaveTypeId, int userId, CancellationToken ct = default)
    {
        if (month < 1 || month > 12)
            return ServiceResult<LeaveBalanceRecalculateResultDto>.Fail("Le mois doit être entre 1 et 12.");

        var balances = await _db.LeaveBalances
            .Where(b => b.EmployeeId == employeeId && b.Year == year && b.Month == month && b.DeletedAt == null)
            .ToListAsync(ct);

        if (balances.Count == 0)
        {
            var employee = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.DeletedAt == null, ct);

            if (employee == null)
                return ServiceResult<LeaveBalanceRecalculateResultDto>.Fail("Employé non trouvé.");

            var effectiveCompanyId = companyId ?? employee.CompanyId;

            var leaveTypesQuery = _db.LeaveTypes
                .AsNoTracking()
                .Where(lt => lt.DeletedAt == null && lt.IsActive);

            if (leaveTypeId.HasValue)
                leaveTypesQuery = leaveTypesQuery.Where(lt => lt.Id == leaveTypeId.Value);
            else
                leaveTypesQuery = leaveTypesQuery.Where(lt => lt.CompanyId == null || lt.CompanyId == effectiveCompanyId);

            var leaveTypes = await leaveTypesQuery.ToListAsync(ct);
            if (leaveTypes.Count == 0)
                return ServiceResult<LeaveBalanceRecalculateResultDto>.Fail("Aucun type de congé actif trouvé.");

            var seedBalances = leaveTypes.Select(lt => new LeaveBalance
            {
                EmployeeId = employeeId,
                CompanyId = effectiveCompanyId,
                LeaveTypeId = lt.Id,
                Year = year,
                Month = month,
                OpeningDays = 0m,
                AccruedDays = 0m,
                UsedDays = 0m,
                CarryInDays = 0m,
                CarryOutDays = 0m,
                ClosingDays = 0m,
                LastRecalculatedAt = null,
                CreatedBy = userId
            }).ToList();

            _db.LeaveBalances.AddRange(seedBalances);
            await _db.SaveChangesAsync(ct);

            balances = await _db.LeaveBalances
                .Where(b => b.EmployeeId == employeeId && b.Year == year && b.Month == month && b.DeletedAt == null)
                .ToListAsync(ct);
        }

        var filtered = balances
            .Where(b => !companyId.HasValue || b.CompanyId == companyId.Value)
            .Where(b => !leaveTypeId.HasValue || b.LeaveTypeId == leaveTypeId.Value)
            .ToList();
        var pairs = filtered.Select(b => (b.CompanyId, b.LeaveTypeId)).Distinct().ToList();

        var recalculatedCount = 0;
        foreach (var (cid, ltid) in pairs)
        {
            var recalc = await _leaveBalanceRecalc.RecalculateRangeThroughMonthAsync(
                cid, employeeId, ltid, year, month, userId, ct);
            if (recalc.Success) recalculatedCount++;
        }

        var first = balances[0];
        if (recalculatedCount > 0)
        {
            await _leaveEventLog.LogEventAsync(
                first.CompanyId, employeeId, null, "BalanceRecalculated",
                null, $"{recalculatedCount} solde(s) recalculé(s) pour {year}-{month:D2}", userId, ct);
        }

        var msg = $"{recalculatedCount} solde(s) recalculé(s)";
        return ServiceResult<LeaveBalanceRecalculateResultDto>.Ok(
            new LeaveBalanceRecalculateResultDto(recalculatedCount, balances.Count, msg));
    }

    public async Task<ServiceResult<LeaveBalanceReadDto>> CreateBalanceAsync(LeaveBalanceCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        // Vérification employé et LeaveType
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null, ct);
        if (!employeeExists) return ServiceResult<LeaveBalanceReadDto>.Fail("Employé non trouvé.");

        var leaveTypeExists = await _db.LeaveTypes.AnyAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null, ct);
        if (!leaveTypeExists) return ServiceResult<LeaveBalanceReadDto>.Fail("Type de congé non trouvé.");

        // Unicité : un seul solde actif par (EmployeeId, LeaveTypeId, Year, Month)
        var balanceExists = await _db.LeaveBalances.AnyAsync(b =>
            b.EmployeeId == dto.EmployeeId && b.LeaveTypeId == dto.LeaveTypeId &&
            b.Year == dto.Year && b.Month == dto.Month && b.DeletedAt == null, ct);
        if (balanceExists)
            return ServiceResult<LeaveBalanceReadDto>.Fail("Un solde existe déjà pour cet employé, ce type de congé et ce mois.");

        var closingDays = dto.OpeningDays + dto.AccruedDays + dto.CarryInDays - dto.UsedDays - dto.CarryOutDays;

        var lb = new LeaveBalance
        {
            EmployeeId = dto.EmployeeId,
            CompanyId = dto.CompanyId,
            LeaveTypeId = dto.LeaveTypeId,
            Year = dto.Year,
            Month = dto.Month,
            OpeningDays = dto.OpeningDays,
            AccruedDays = dto.AccruedDays,
            UsedDays = dto.UsedDays,
            CarryInDays = dto.CarryInDays,
            CarryOutDays = dto.CarryOutDays,
            ClosingDays = closingDays,
            CarryoverExpiresOn = dto.CarryoverExpiresOn,
            LastRecalculatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
        _db.LeaveBalances.Add(lb);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveBalanceReadDto>.Ok(MapBalance(lb));
    }

    public async Task<ServiceResult<LeaveBalanceReadDto>> PatchBalanceAsync(int id, LeaveBalancePatchDto dto, int updatedBy, CancellationToken ct = default)
    {
        var lb = await _db.LeaveBalances.FindAsync(new object[] { id }, ct);
        if (lb == null || lb.DeletedAt != null) return ServiceResult<LeaveBalanceReadDto>.Fail("Solde introuvable.");

        if (dto.OpeningDays != null) lb.OpeningDays = dto.OpeningDays.Value;
        if (dto.AccruedDays != null) lb.AccruedDays = dto.AccruedDays.Value;
        if (dto.UsedDays != null) lb.UsedDays = dto.UsedDays.Value;
        if (dto.CarryInDays != null) lb.CarryInDays = dto.CarryInDays.Value;
        if (dto.CarryOutDays != null) lb.CarryOutDays = dto.CarryOutDays.Value;
        if (dto.CarryoverExpiresOn != null) lb.CarryoverExpiresOn = dto.CarryoverExpiresOn;
        if (dto.ClosingDays != null)
            lb.ClosingDays = dto.ClosingDays.Value;
        else
            // Recalcul automatique si ClosingDays non fourni
            lb.ClosingDays = lb.OpeningDays + lb.AccruedDays + lb.CarryInDays - lb.UsedDays - lb.CarryOutDays;

        lb.LastRecalculatedAt = DateTimeOffset.UtcNow;
        lb.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveBalanceReadDto>.Ok(MapBalance(lb));
    }

    public async Task<ServiceResult> DeleteBalanceAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var lb = await _db.LeaveBalances.FindAsync(new object[] { id }, ct);
        if (lb == null) return ServiceResult.Fail("Solde introuvable.");
        lb.DeletedAt = DateTimeOffset.UtcNow;
        lb.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── LeaveCarryOverAgreement ───────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<LeaveCarryOverAgreementReadDto>>> GetCarryOverAgreementsAsync(int employeeId, CancellationToken ct = default)
    {
        var list = await _db.LeaveCarryOverAgreements
            .Where(a => a.EmployeeId == employeeId && a.DeletedAt == null)
            .Select(a => MapCarryOver(a)).ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveCarryOverAgreementReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<LeaveCarryOverAgreementReadDto>> GetCarryOverByIdAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.LeaveCarryOverAgreements.FindAsync(new object[] { id }, ct);
        if (a == null || a.DeletedAt != null) return ServiceResult<LeaveCarryOverAgreementReadDto>.Fail("Accord introuvable.");
        return ServiceResult<LeaveCarryOverAgreementReadDto>.Ok(MapCarryOver(a));
    }

    public async Task<ServiceResult<LeaveCarryOverAgreementReadDto>> CreateCarryOverAgreementAsync(LeaveCarryOverAgreementCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        // Vérifications
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null, ct);
        if (!employeeExists) return ServiceResult<LeaveCarryOverAgreementReadDto>.Fail("Employé non trouvé.");

        var leaveType = await _db.LeaveTypes
            .Include(lt => lt.Policies)
            .FirstOrDefaultAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null, ct);
        if (leaveType == null) return ServiceResult<LeaveCarryOverAgreementReadDto>.Fail("Type de congé non trouvé.");

        if (dto.FromYear >= dto.ToYear)
            return ServiceResult<LeaveCarryOverAgreementReadDto>.Fail("FromYear doit être antérieur à ToYear.");

        // Vérification AllowCarryover et MaxCarryoverYears via la policy
        var policy = await _db.LeaveTypePolicies
            .FirstOrDefaultAsync(p => p.LeaveTypeId == dto.LeaveTypeId && p.CompanyId == dto.CompanyId && p.DeletedAt == null, ct);
        if (policy != null && !policy.AllowCarryover)
            return ServiceResult<LeaveCarryOverAgreementReadDto>.Fail("Le type de congé n'autorise pas le report de congés.");
        if (policy != null)
        {
            var yearsDiff = dto.ToYear - dto.FromYear;
            if (yearsDiff > policy.MaxCarryoverYears)
                return ServiceResult<LeaveCarryOverAgreementReadDto>.Fail($"Le report ne peut pas dépasser {policy.MaxCarryoverYears} année(s).");
        }

        var a = new LeaveCarryOverAgreement
        {
            EmployeeId = dto.EmployeeId,
            CompanyId = dto.CompanyId,
            LeaveTypeId = dto.LeaveTypeId,
            FromYear = dto.FromYear,
            ToYear = dto.ToYear,
            AgreementDate = dto.AgreementDate,
            AgreementDocRef = dto.AgreementDocRef?.Trim(),
            CreatedBy = createdBy
        };
        _db.LeaveCarryOverAgreements.Add(a);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveCarryOverAgreementReadDto>.Ok(MapCarryOver(a));
    }

    public async Task<ServiceResult<LeaveCarryOverAgreementReadDto>> PatchCarryOverAgreementAsync(int id, LeaveCarryOverAgreementPatchDto dto, int updatedBy, CancellationToken ct = default)
    {
        var a = await _db.LeaveCarryOverAgreements.FindAsync(new object[] { id }, ct);
        if (a == null || a.DeletedAt != null) return ServiceResult<LeaveCarryOverAgreementReadDto>.Fail("Accord introuvable.");

        if (dto.FromYear != null) a.FromYear = dto.FromYear.Value;
        if (dto.ToYear != null) a.ToYear = dto.ToYear.Value;

        if (a.FromYear >= a.ToYear)
            return ServiceResult<LeaveCarryOverAgreementReadDto>.Fail("FromYear doit être antérieur à ToYear.");

        if (dto.AgreementDate != null) a.AgreementDate = dto.AgreementDate.Value;
        if (dto.AgreementDocRef != null) a.AgreementDocRef = dto.AgreementDocRef.Trim();

        a.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveCarryOverAgreementReadDto>.Ok(MapCarryOver(a));
    }

    public async Task<ServiceResult> DeleteCarryOverAgreementAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var a = await _db.LeaveCarryOverAgreements.FindAsync(new object[] { id }, ct);
        if (a == null) return ServiceResult.Fail("Accord introuvable.");
        a.DeletedAt = DateTimeOffset.UtcNow;
        a.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── LeaveRequestAttachment ────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<LeaveRequestAttachmentReadDto>>> GetAttachmentsAsync(int leaveRequestId, CancellationToken ct = default)
    {
        var list = await _db.LeaveRequestAttachments
            .Where(a => a.LeaveRequestId == leaveRequestId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => MapAttachment(a)).ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveRequestAttachmentReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<LeaveRequestAttachmentReadDto>> GetAttachmentByIdAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.LeaveRequestAttachments.FindAsync(new object[] { id }, ct);
        if (a == null) return ServiceResult<LeaveRequestAttachmentReadDto>.Fail("Pièce jointe introuvable.");
        return ServiceResult<LeaveRequestAttachmentReadDto>.Ok(MapAttachment(a));
    }

    public async Task<ServiceResult<LeaveRequestAttachmentReadDto>> CreateAttachmentAsync(LeaveRequestAttachmentCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        // Vérifier que la LeaveRequest existe
        var leaveRequest = await _db.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == dto.LeaveRequestId && lr.DeletedAt == null, ct);
        if (leaveRequest == null) return ServiceResult<LeaveRequestAttachmentReadDto>.Fail("Demande de congé non trouvée.");

        var a = new LeaveRequestAttachment
        {
            LeaveRequestId = dto.LeaveRequestId,
            FileName = dto.FileName,
            FilePath = dto.FilePath,
            FileType = dto.FileType,
            CreatedBy = createdBy
        };
        _db.LeaveRequestAttachments.Add(a);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveRequestAttachmentReadDto>.Ok(MapAttachment(a));
    }

    public async Task<ServiceResult<(byte[] content, string fileName, string contentType)>> GetAttachmentDownloadAsync(int attachmentId, CancellationToken ct = default)
    {
        var a = await _db.LeaveRequestAttachments.FindAsync(new object[] { attachmentId }, ct);
        if (a == null) return ServiceResult<(byte[], string, string)>.Fail("Pièce jointe introuvable.");
        if (string.IsNullOrEmpty(a.FilePath) || !System.IO.File.Exists(a.FilePath))
            return ServiceResult<(byte[], string, string)>.Ok((Array.Empty<byte>(), a.FileName ?? "attachment", a.FileType ?? "application/octet-stream"));
        var bytes = await System.IO.File.ReadAllBytesAsync(a.FilePath, ct);
        return ServiceResult<(byte[], string, string)>.Ok((bytes, a.FileName ?? "attachment", a.FileType ?? "application/octet-stream"));
    }

    public async Task<ServiceResult> DeleteAttachmentAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var a = await _db.LeaveRequestAttachments.FindAsync(new object[] { id }, ct);
        if (a == null) return ServiceResult.Fail("Pièce jointe introuvable.");

        // Suppression du fichier physique si présent
        if (!string.IsNullOrEmpty(a.FilePath) && System.IO.File.Exists(a.FilePath))
        {
            try { System.IO.File.Delete(a.FilePath); }
            catch { /* log si nécessaire */ }
        }

        a.DeletedAt = DateTimeOffset.UtcNow;
        a.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── LeaveRequestExemption ─────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<LeaveRequestExemptionReadDto>>> GetExemptionsAsync(int leaveRequestId, CancellationToken ct = default)
    {
        var list = await _db.LeaveRequestExemptions
            .Where(e => e.LeaveRequestId == leaveRequestId && e.DeletedAt == null)
            .OrderBy(e => e.ExemptionDate)
            .Select(e => MapExemption(e)).ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveRequestExemptionReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<LeaveRequestExemptionReadDto>> GetExemptionByIdAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.LeaveRequestExemptions.FindAsync(new object[] { id }, ct);
        if (e == null || e.DeletedAt != null) return ServiceResult<LeaveRequestExemptionReadDto>.Fail("Exemption introuvable.");
        return ServiceResult<LeaveRequestExemptionReadDto>.Ok(MapExemption(e));
    }

    public async Task<ServiceResult<LeaveRequestExemptionReadDto>> CreateExemptionAsync(LeaveRequestExemptionCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var leaveRequest = await _db.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == dto.LeaveRequestId && lr.DeletedAt == null, ct);
        if (leaveRequest == null) return ServiceResult<LeaveRequestExemptionReadDto>.Fail("Demande de congé non trouvée.");

        // Validation: ExemptionDate dans la période de la demande
        if (dto.ExemptionDate < leaveRequest.StartDate || dto.ExemptionDate > leaveRequest.EndDate)
            return ServiceResult<LeaveRequestExemptionReadDto>.Fail("La date d'exemption doit être comprise entre les dates de début et de fin de la demande.");

        // Validation: HolidayId obligatoire si ReasonType = Holiday
        if (dto.ReasonType == LeaveExemptionReasonType.Holiday && !dto.HolidayId.HasValue)
            return ServiceResult<LeaveRequestExemptionReadDto>.Fail("HolidayId est obligatoire pour le type Holiday.");

        // Validation: EmployeeAbsenceId obligatoire si ReasonType = EmployeeAbsence
        if (dto.ReasonType == LeaveExemptionReasonType.EmployeeAbsence && !dto.EmployeeAbsenceId.HasValue)
            return ServiceResult<LeaveRequestExemptionReadDto>.Fail("EmployeeAbsenceId est obligatoire pour le type EmployeeAbsence.");

        // Unicité par date
        var exemptionExists = await _db.LeaveRequestExemptions
            .AnyAsync(e => e.LeaveRequestId == dto.LeaveRequestId && e.ExemptionDate == dto.ExemptionDate && e.DeletedAt == null, ct);
        if (exemptionExists)
            return ServiceResult<LeaveRequestExemptionReadDto>.Fail("Une exemption existe déjà pour cette date.");

        var e = new LeaveRequestExemption
        {
            LeaveRequestId = dto.LeaveRequestId,
            ExemptionDate = dto.ExemptionDate,
            ReasonType = dto.ReasonType,
            CountsAsLeaveDay = dto.CountsAsLeaveDay,
            HolidayId = dto.HolidayId,
            EmployeeAbsenceId = dto.EmployeeAbsenceId,
            Note = dto.Note?.Trim(),
            CreatedBy = createdBy
        };
        _db.LeaveRequestExemptions.Add(e);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveRequestExemptionReadDto>.Ok(MapExemption(e));
    }

    public async Task<ServiceResult<LeaveRequestExemptionReadDto>> PatchExemptionAsync(int id, LeaveRequestExemptionPatchDto dto, int updatedBy, CancellationToken ct = default)
    {
        var e = await _db.LeaveRequestExemptions
            .Include(x => x.LeaveRequest)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (e == null) return ServiceResult<LeaveRequestExemptionReadDto>.Fail("Exemption introuvable.");

        // Validation date si changement
        if (dto.ExemptionDate != null)
        {
            if (dto.ExemptionDate < e.LeaveRequest.StartDate || dto.ExemptionDate > e.LeaveRequest.EndDate)
                return ServiceResult<LeaveRequestExemptionReadDto>.Fail("La date d'exemption doit être comprise entre les dates de la demande.");
            e.ExemptionDate = dto.ExemptionDate.Value;
        }

        if (dto.ReasonType != null) e.ReasonType = dto.ReasonType.Value;
        if (dto.CountsAsLeaveDay != null) e.CountsAsLeaveDay = dto.CountsAsLeaveDay.Value;
        if (dto.HolidayId != null) e.HolidayId = dto.HolidayId;
        if (dto.EmployeeAbsenceId != null) e.EmployeeAbsenceId = dto.EmployeeAbsenceId;
        if (dto.Note != null) e.Note = dto.Note.Trim();

        e.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveRequestExemptionReadDto>.Ok(MapExemption(e));
    }

    public async Task<ServiceResult> DeleteExemptionAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var e = await _db.LeaveRequestExemptions.FindAsync(new object[] { id }, ct);
        if (e == null) return ServiceResult.Fail("Exemption introuvable.");
        // Soft delete (cohérence avec le reste)
        e.DeletedAt = DateTimeOffset.UtcNow;
        e.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── LeaveAuditLog ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<LeaveAuditLogReadDto>>> GetAuditLogsAsync(
        int? companyId, int? leaveRequestId, CancellationToken ct = default)
    {
        var q = _db.LeaveAuditLogs.AsQueryable();
        if (companyId.HasValue) q = q.Where(l => l.CompanyId == companyId.Value);
        if (leaveRequestId.HasValue) q = q.Where(l => l.LeaveRequestId == leaveRequestId.Value);
        var list = await q.OrderByDescending(l => l.CreatedAt)
            .Take(1000)
            .Select(l => MapAuditLog(l)).ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveAuditLogReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<LeaveAuditLogReadDto>> GetAuditLogByIdAsync(int id, CancellationToken ct = default)
    {
        var l = await _db.LeaveAuditLogs.FindAsync(new object[] { id }, ct);
        if (l == null) return ServiceResult<LeaveAuditLogReadDto>.Fail("Log d'audit introuvable.");
        return ServiceResult<LeaveAuditLogReadDto>.Ok(MapAuditLog(l));
    }

    public async Task<ServiceResult<IEnumerable<LeaveAuditLogReadDto>>> GetAuditLogsByEmployeeAsync(int employeeId, CancellationToken ct = default)
    {
        var list = await _db.LeaveAuditLogs
            .Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(500)
            .Select(l => MapAuditLog(l)).ToListAsync(ct);
        return ServiceResult<IEnumerable<LeaveAuditLogReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<LeaveAuditLogReadDto>> CreateAuditLogAsync(LeaveAuditLogCreateDto dto, CancellationToken ct = default)
    {
        var log = new LeaveAuditLog
        {
            CompanyId = dto.CompanyId,
            EmployeeId = dto.EmployeeId,
            LeaveRequestId = dto.LeaveRequestId,
            EventName = dto.EventName,
            OldValue = dto.OldValue,
            NewValue = dto.NewValue,
            CreatedBy = dto.CreatedBy ?? 0
        };
        _db.LeaveAuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<LeaveAuditLogReadDto>.Ok(MapAuditLog(log));
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static LeaveTypeReadDto MapLeaveType(LeaveType lt) => new()
    {
        Id = lt.Id,
        LeaveCode = lt.LeaveCode,
        LeaveName = lt.LeaveNameFr ?? string.Empty,
        LeaveDescription = lt.LeaveDescription ?? string.Empty,
        Scope = lt.Scope,
        CompanyId = lt.CompanyId,
        CompanyName = lt.Company?.CompanyName ?? string.Empty,
        IsActive = lt.IsActive,
        CreatedAt = lt.CreatedAt
    };

    private static LeaveTypePolicyReadDto MapPolicy(LeaveTypePolicy p) => new()
    {
        Id = p.Id,
        CompanyId = p.CompanyId,
        LeaveTypeId = p.LeaveTypeId,
        IsEnabled = p.IsEnabled,
        AccrualMethod = p.AccrualMethod,
        DaysPerMonthAdult = p.DaysPerMonthAdult,
        DaysPerMonthMinor = p.DaysPerMonthMinor,
        BonusDaysPerYearAfter5Years = p.BonusDaysPerYearAfter5Years,
        RequiresEligibility6Months = p.RequiresEligibility6Months,
        RequiresBalance = p.RequiresBalance,
        AnnualCapDays = p.AnnualCapDays,
        AllowCarryover = p.AllowCarryover,
        MaxCarryoverYears = p.MaxCarryoverYears,
        MinConsecutiveDays = p.MinConsecutiveDays,
        UseWorkingCalendar = p.UseWorkingCalendar
    };

    private static LeaveTypeLegalRuleReadDto MapLegalRule(LeaveTypeLegalRule r) => new()
    {
        Id = r.Id,
        LeaveTypeId = r.LeaveTypeId,
        EventCaseCode = r.EventCaseCode,
        Description = r.Description,
        DaysGranted = r.DaysGranted,
        LegalArticle = r.LegalArticle,
        CanBeDiscontinuous = r.CanBeDiscountinuous,
        MustBeUsedWithinDays = r.MustBeUsedWithinDays
    };

    private static LeaveRequestReadDto MapLeaveRequest(LeaveRequest lr) => new()
    {
        Id = lr.Id,
        EmployeeId = lr.EmployeeId,
        CompanyId = lr.CompanyId,
        LeaveTypeId = lr.LeaveTypeId,
        LeaveTypeCode = lr.LeaveType?.LeaveCode ?? string.Empty,
        LeaveTypeName = lr.LeaveType?.LeaveNameFr ?? string.Empty,
        LeaveTypePolicyId = lr.PolicyId ?? 0,
        LevaeTypePolicyCode = string.Empty,
        LeaveTypePolicyName = string.Empty,
        LegalRuleId = lr.LegalRuleId,
        LegalCaseCode = lr.LegalRule?.EventCaseCode,
        LegalCaseDescription = lr.LegalRule?.Description,
        LegalDaysGranted = lr.LegalRule?.DaysGranted,
        LegalArticle = lr.LegalRule?.LegalArticle,
        StartDate = lr.StartDate,
        EndDate = lr.EndDate,
        Status = lr.Status,
        CalendarDays = lr.CalendarDays,
        WorkingDaysDeducted = lr.WorkingDaysDeducted,
        HasMinConsecutiveBlock = lr.HasMinConsecutiveBlock,
        ComputationVersion = lr.ComputationVersion,
        EmployeeNote = lr.EmployeeNote,
        ManagerNote = lr.ManagerNote,
        RequestedAt = lr.RequestedAt,
        SubmittedAt = lr.SubmittedAt,
        DecisionAt = lr.DecisionAt,
        DecisionBy = lr.DecisionBy,
        DecisionComment = lr.DecisionComment,
        IsRenounced = lr.IsRenounced,
        CreatedAt = lr.CreatedAt
    };

    private static LeaveBalanceReadDto MapBalance(LeaveBalance lb, DateOnly? asOf = null)
    {
        var refDate = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var isExpired = lb.CarryoverExpiresOn.HasValue && lb.CarryoverExpiresOn.Value < refDate;
        return new LeaveBalanceReadDto
        {
            Id = lb.Id,
            EmployeeId = lb.EmployeeId,
            CompanyId = lb.CompanyId,
            LeaveTypeId = lb.LeaveTypeId,
            Year = lb.Year,
            Month = lb.Month,
            BalanceExpiresOn = lb.GetBalanceExpiresOn(),
            OpeningDays = lb.OpeningDays,
            AccruedDays = lb.AccruedDays,
            UsedDays = lb.UsedDays,
            CarryInDays = lb.CarryInDays,
            CarryOutDays = lb.CarryOutDays,
            ClosingDays = lb.ClosingDays,
            CarryoverExpiresOn = lb.CarryoverExpiresOn,
            IsCarryoverExpired = isExpired,
            LastRecalculatedAt = lb.LastRecalculatedAt,
            CreatedAt = lb.CreatedAt,
            UpdatedAt = lb.UpdatedAt
        };
    }

    private static LeaveCarryOverAgreementReadDto MapCarryOver(LeaveCarryOverAgreement a) => new()
    {
        Id = a.Id,
        EmployeeId = a.EmployeeId,
        CompanyId = a.CompanyId,
        LeaveTypeId = a.LeaveTypeId,
        FromYear = a.FromYear,
        ToYear = a.ToYear,
        AgreementDate = a.AgreementDate,
        AgreementDocRef = a.AgreementDocRef,
        CreatedAt = a.CreatedAt
    };

    private static LeaveRequestAttachmentReadDto MapAttachment(LeaveRequestAttachment a) => new()
    {
        Id = a.Id,
        LeaveRequestId = a.LeaveRequestId,
        FileName = a.FileName,
        FilePath = a.FilePath,
        FileType = a.FileType,
        UploadedAt = a.CreatedAt,
        UploadedBy = a.CreatedBy
    };

    private static LeaveRequestExemptionReadDto MapExemption(LeaveRequestExemption e) => new()
    {
        Id = e.Id,
        LeaveRequestId = e.LeaveRequestId,
        ExemptionDate = e.ExemptionDate,
        ReasonType = e.ReasonType,
        CountsAsLeaveDay = e.CountsAsLeaveDay,
        HolidayId = e.HolidayId,
        EmployeeAbsenceId = e.EmployeeAbsenceId,
        Note = e.Note,
        CreatedAt = e.CreatedAt,
        CreatedBy = e.CreatedBy
    };

    private static LeaveAuditLogReadDto MapAuditLog(LeaveAuditLog l) => new()
    {
        Id = l.Id,
        CompanyId = l.CompanyId,
        EmployeeId = l.EmployeeId,
        LeaveRequestId = l.LeaveRequestId,
        EventName = l.EventName,
        OldValue = l.OldValue,
        NewValue = l.NewValue,
        CreatedAt = l.CreatedAt,
        CreatedBy = l.CreatedBy
    };
}

// ── Sub-service facades ───────────────────────────────────────────────────────

public class LeaveTypeService : ILeaveTypeService
{
    private readonly ILeaveService _svc;
    public LeaveTypeService(ILeaveService svc) => _svc = svc;
    public Task<ServiceResult<IEnumerable<LeaveTypeReadDto>>> GetAllAsync(int? companyId, CancellationToken ct = default) => _svc.GetLeaveTypesAsync(companyId, ct);
    public Task<ServiceResult<LeaveTypeReadDto>> GetByIdAsync(int id, CancellationToken ct = default) => _svc.GetLeaveTypeByIdAsync(id, ct);
    public Task<ServiceResult<LeaveTypeReadDto>> CreateAsync(LeaveTypeCreateDto dto, int createdBy, CancellationToken ct = default) => _svc.CreateLeaveTypeAsync(dto, createdBy, ct);
    public Task<ServiceResult<LeaveTypeReadDto>> PatchAsync(int id, LeaveTypePatchDto dto, int updatedBy, CancellationToken ct = default) => _svc.PatchLeaveTypeAsync(id, dto, updatedBy, ct);
    public Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default) => _svc.DeleteLeaveTypeAsync(id, deletedBy, ct);
    public Task<ServiceResult<IEnumerable<LeaveTypePolicyReadDto>>> GetPoliciesAsync(int? companyId, int? leaveTypeId, CancellationToken ct = default) => _svc.GetPoliciesAsync(companyId, leaveTypeId, ct);
    public Task<ServiceResult<LeaveTypePolicyReadDto>> CreatePolicyAsync(LeaveTypePolicyCreateDto dto, int createdBy, CancellationToken ct = default) => _svc.CreatePolicyAsync(dto, createdBy, ct);
    public Task<ServiceResult<LeaveTypePolicyReadDto>> PatchPolicyAsync(int id, LeaveTypePolicyPatchDto dto, int updatedBy, CancellationToken ct = default) => _svc.PatchPolicyAsync(id, dto, updatedBy, ct);
    public Task<ServiceResult> DeletePolicyAsync(int id, int deletedBy, CancellationToken ct = default) => _svc.DeletePolicyAsync(id, deletedBy, ct);
    public Task<ServiceResult<IEnumerable<LeaveTypeLegalRuleReadDto>>> GetLegalRulesAsync(int? leaveTypeId, CancellationToken ct = default) => _svc.GetLegalRulesAsync(leaveTypeId, ct);
    public Task<ServiceResult<LeaveTypeLegalRuleReadDto>> CreateLegalRuleAsync(LeaveTypeLegalRuleCreateDto dto, int createdBy, CancellationToken ct = default) => _svc.CreateLegalRuleAsync(dto, createdBy, ct);
    public Task<ServiceResult> DeleteLegalRuleAsync(int id, int deletedBy, CancellationToken ct = default) => _svc.DeleteLegalRuleAsync(id, deletedBy, ct);
}

public class LeaveBalanceService : ILeaveBalanceService
{
    private readonly ILeaveService _svc;
    public LeaveBalanceService(ILeaveService svc) => _svc = svc;
    public Task<ServiceResult<IEnumerable<LeaveBalanceReadDto>>> GetByEmployeeAsync(int employeeId, int? leaveTypeId, CancellationToken ct = default) => _svc.GetBalancesAsync(employeeId, leaveTypeId, ct);
    public Task<ServiceResult<LeaveBalanceReadDto>> CreateAsync(LeaveBalanceCreateDto dto, int createdBy, CancellationToken ct = default) => _svc.CreateBalanceAsync(dto, createdBy, ct);
    public Task<ServiceResult<LeaveBalanceReadDto>> PatchAsync(int id, LeaveBalancePatchDto dto, int updatedBy, CancellationToken ct = default) => _svc.PatchBalanceAsync(id, dto, updatedBy, ct);
    public Task<ServiceResult<IEnumerable<LeaveCarryOverAgreementReadDto>>> GetCarryOverAgreementsAsync(int employeeId, CancellationToken ct = default) => _svc.GetCarryOverAgreementsAsync(employeeId, ct);
    public Task<ServiceResult<LeaveCarryOverAgreementReadDto>> CreateCarryOverAgreementAsync(LeaveCarryOverAgreementCreateDto dto, int createdBy, CancellationToken ct = default) => _svc.CreateCarryOverAgreementAsync(dto, createdBy, ct);
    public Task<ServiceResult> DeleteCarryOverAgreementAsync(int id, int deletedBy, CancellationToken ct = default) => _svc.DeleteCarryOverAgreementAsync(id, deletedBy, ct);
}

public class LeaveAuditLogService : ILeaveAuditLogService
{
    private readonly ILeaveService _svc;
    public LeaveAuditLogService(ILeaveService svc) => _svc = svc;
    public Task<ServiceResult<IEnumerable<LeaveAuditLogReadDto>>> GetAsync(int? companyId, int? leaveRequestId, CancellationToken ct = default) => _svc.GetAuditLogsAsync(companyId, leaveRequestId, ct);
    public Task<ServiceResult<LeaveAuditLogReadDto>> CreateAsync(LeaveAuditLogCreateDto dto, CancellationToken ct = default) => _svc.CreateAuditLogAsync(dto, ct);
}