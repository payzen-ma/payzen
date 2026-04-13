using Microsoft.EntityFrameworkCore;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Leave;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Leave;

/// <summary>
/// Recalcul des soldes par mois (politique entreprise/global, accrual, jours utilisés, report).
/// Chaîne chronologique depuis le premier mois de contrat jusqu'au mois cible (comme l'ancien contrôleur métier).
/// </summary>
public sealed class LeaveBalanceRecalculationService : ILeaveBalanceRecalculationService
{
    private readonly AppDbContext _db;

    public LeaveBalanceRecalculationService(AppDbContext db) => _db = db;

    private static void GetPreviousMonth(int year, int month, out int prevYear, out int prevMonth)
    {
        if (month == 1)
        {
            prevYear = year - 1;
            prevMonth = 12;
        }
        else
        {
            prevYear = year;
            prevMonth = month - 1;
        }
    }

    /// <summary>
    /// Report depuis le solde du mois précédent déjà en base (pas de récursion : la chaîne forward garantit que M-1 est à jour).
    /// </summary>
    private async Task<decimal> ComputeCarryInFromPreviousMonthAsync(
        int companyId,
        int employeeId,
        int leaveTypeId,
        int year,
        int month,
        LeaveTypePolicy policy,
        DateOnly refDateForExpiry,
        CancellationToken ct)
    {
        GetPreviousMonth(year, month, out int prevYear, out int prevMonth);

        var contractStart = await GetEmployeeContractStartAsync(employeeId, ct);
        if (contractStart == null)
            return 0m;

        var contractYear = contractStart.Value.Year;
        var contractMonth = contractStart.Value.Month;
        if (prevYear < contractYear || (prevYear == contractYear && prevMonth < contractMonth))
            return 0m;

        var prevBalance = await GetExistingBalanceAsync(companyId, employeeId, leaveTypeId, prevYear, prevMonth, ct);
        if (prevBalance == null)
            return 0m;

        if (prevBalance.CarryoverExpiresOn.HasValue && prevBalance.CarryoverExpiresOn.Value < refDateForExpiry)
            return 0m;

        if (!policy.AllowCarryover || policy.MaxCarryoverYears <= 0)
            return 0m;

        return prevBalance.ClosingDays < 0 ? 0m : prevBalance.ClosingDays;
    }

    public Task<LeaveBalanceMonthRecalcResult> RecalculateAsync(
        int companyId,
        int employeeId,
        int leaveTypeId,
        DateOnly asOfDate,
        int userId,
        CancellationToken ct = default,
        DateOnly? referenceDateForExpiry = null)
        => RecalculateRangeThroughMonthAsync(
            companyId, employeeId, leaveTypeId, asOfDate.Year, asOfDate.Month, userId, ct, referenceDateForExpiry);

    public async Task<LeaveBalanceMonthRecalcResult> RecalculateRangeThroughMonthAsync(
        int companyId,
        int employeeId,
        int leaveTypeId,
        int endYear,
        int endMonth,
        int userId,
        CancellationToken ct = default,
        DateOnly? referenceDateForExpiry = null)
    {
        if (endMonth is < 1 or > 12)
            return LeaveBalanceMonthRecalcResult.Fail("Le mois doit être entre 1 et 12.");

        var contractStart = await GetEmployeeContractStartAsync(employeeId, ct);
        if (contractStart == null)
            return LeaveBalanceMonthRecalcResult.Fail("Aucun contrat actif trouvé pour l'employé.");

        var contractFirstMonth = new DateOnly(contractStart.Value.Year, contractStart.Value.Month, 1);
        var chainEnd = new DateOnly(endYear, endMonth, 1);
        var isAnnualLeaveType = await _db.LeaveTypes
            .AsNoTracking()
            .AnyAsync(lt => lt.Id == leaveTypeId && lt.DeletedAt == null && lt.LeaveCode == "ANNUAL", ct);
        var employeeAnnualOpeningDays = await _db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.DeletedAt == null)
            .Select(e => (decimal?)e.AnnualLeaveOpeningDays)
            .FirstOrDefaultAsync(ct) ?? 0m;
        var employeeAnnualOpeningEffectiveFrom = await _db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.DeletedAt == null)
            .Select(e => e.AnnualLeaveOpeningEffectiveFrom)
            .FirstOrDefaultAsync(ct);

        DateOnly? annualOpeningMonth = null;
        if (isAnnualLeaveType && employeeAnnualOpeningDays > 0m)
        {
            // Fallback compatibilité: anciennes données sans mois d'effet persistant.
            // On prend le mois du recalcul courant pour injecter la base utilisateur.
            annualOpeningMonth = employeeAnnualOpeningEffectiveFrom.HasValue
                ? new DateOnly(employeeAnnualOpeningEffectiveFrom.Value.Year, employeeAnnualOpeningEffectiveFrom.Value.Month, 1)
                : chainEnd;
        }

        // Workflow métier demandé:
        // 1) injecter la valeur d'ouverture au premier mois de contrat,
        // 2) recalculer ensuite chaque mois en chaîne (M dépend de M-1).
        // Pour garantir ce comportement, le congé annuel repart toujours du début contrat.
        DateOnly chainStart;
        if (isAnnualLeaveType)
        {
            var effectiveMonth = annualOpeningMonth ?? contractFirstMonth;
            chainStart = effectiveMonth < contractFirstMonth ? contractFirstMonth : effectiveMonth;
        }
        else
        {
            // Mode incrémental conservé pour les autres types de congé.
            var latestExisting = await _db.LeaveBalances
                .AsNoTracking()
                .Where(b =>
                    b.EmployeeId == employeeId &&
                    b.CompanyId == companyId &&
                    b.LeaveTypeId == leaveTypeId &&
                    b.DeletedAt == null)
                .OrderByDescending(b => b.Year)
                .ThenByDescending(b => b.Month)
                .Select(b => new { b.Year, b.Month })
                .FirstOrDefaultAsync(ct);

            chainStart = latestExisting == null
                ? contractFirstMonth
                : new DateOnly(latestExisting.Year, latestExisting.Month, 1);
        }

        // Ne jamais recalculer avant le mois du début de contrat.
        if (chainStart < contractFirstMonth)
            chainStart = contractFirstMonth;

        if (chainEnd < chainStart)
            return LeaveBalanceMonthRecalcResult.Ok();

        for (var cursor = chainStart; cursor <= chainEnd; cursor = cursor.AddMonths(1))
        {
            var y = cursor.Year;
            var m = cursor.Month;
            var asOfEndOfMonth = new DateOnly(y, m, DateTime.DaysInMonth(y, m));
            var refDate = referenceDateForExpiry ?? asOfEndOfMonth;

            var policy = await ResolvePolicyAsync(companyId, leaveTypeId, asOfEndOfMonth, ct);
            if (policy == null)
                return LeaveBalanceMonthRecalcResult.Fail("Aucune politique active trouvée pour ce type de congé (company/global).");

            var openingDaysForCreate = 0m;
            var isAnnualOpeningMonth = isAnnualLeaveType
                && annualOpeningMonth.HasValue
                && y == annualOpeningMonth.Value.Year
                && m == annualOpeningMonth.Value.Month;
            if (isAnnualOpeningMonth && employeeAnnualOpeningDays > 0m)
                openingDaysForCreate = employeeAnnualOpeningDays;

            var balance = await GetOrCreateBalanceAsync(companyId, employeeId, leaveTypeId, y, m, userId, ct, openingDaysForCreate);

            // Injecte la base "congé annuel initial" même si la ligne du mois existe déjà.
            // Sans cela, un premier recalcul qui a créé OpeningDays=0 empêcherait
            // la valeur d'ouverture configurée sur l'employé d'avoir un effet.
            if (isAnnualOpeningMonth)
                balance.OpeningDays = employeeAnnualOpeningDays > 0m ? employeeAnnualOpeningDays : 0m;

            balance.AccruedDays = ComputeAccruedDaysForMonth(policy, contractStart.Value, y, m);
            balance.UsedDays = await ComputeUsedDaysAsync(companyId, employeeId, leaveTypeId, y, m, ct);
            balance.CarryInDays = isAnnualOpeningMonth
                ? 0m
                : await ComputeCarryInFromPreviousMonthAsync(
                    companyId, employeeId, leaveTypeId, y, m, policy, refDate, ct);

            balance.CarryOutDays = 0m;
            balance.ClosingDays = ComputeClosingDays(balance);

            if (policy.AnnualCapDays > 0 && balance.AccruedDays > policy.AnnualCapDays)
            {
                balance.AccruedDays = policy.AnnualCapDays;
                balance.ClosingDays = ComputeClosingDays(balance);
            }

            if (policy.AllowCarryover && policy.AnnualCapDays > 0 && balance.ClosingDays > policy.AnnualCapDays)
            {
                balance.CarryOutDays = balance.ClosingDays - policy.AnnualCapDays;
                balance.ClosingDays = ComputeClosingDays(balance);
            }

            balance.CarryoverExpiresOn = balance.GetBalanceExpiresOn();
            balance.LastRecalculatedAt = DateTimeOffset.UtcNow;
            balance.UpdatedAt = DateTimeOffset.UtcNow;
            balance.UpdatedBy = userId;

            await _db.SaveChangesAsync(ct);
        }

        return LeaveBalanceMonthRecalcResult.Ok();
    }

    private async Task<LeaveTypePolicy?> ResolvePolicyAsync(int companyId, int leaveTypeId, DateOnly asOfDate, CancellationToken ct)
    {
        var companyPolicy = await _db.LeaveTypePolicies
            .AsNoTracking()
            .Where(p => p.DeletedAt == null && p.IsEnabled)
            .Where(p => p.LeaveTypeId == leaveTypeId && p.CompanyId == companyId)
            .Where(p => p.EffectiveFrom == null || p.EffectiveFrom <= asOfDate)
            .Where(p => p.EffectiveTo == null || p.EffectiveTo >= asOfDate)
            .OrderByDescending(p => p.EffectiveFrom ?? new DateOnly(1900, 1, 1))
            .FirstOrDefaultAsync(ct);

        if (companyPolicy != null)
            return companyPolicy;

        return await _db.LeaveTypePolicies
            .AsNoTracking()
            .Where(p => p.DeletedAt == null && p.IsEnabled)
            .Where(p => p.LeaveTypeId == leaveTypeId && p.CompanyId == null)
            .Where(p => p.EffectiveFrom == null || p.EffectiveFrom <= asOfDate)
            .Where(p => p.EffectiveTo == null || p.EffectiveTo >= asOfDate)
            .OrderByDescending(p => p.EffectiveFrom ?? new DateOnly(1900, 1, 1))
            .FirstOrDefaultAsync(ct);
    }

    private async Task<DateOnly?> GetEmployeeContractStartAsync(int employeeId, CancellationToken ct)
    {
        var startDate = await _db.EmployeeContracts
            .AsNoTracking()
            .Where(c => c.EmployeeId == employeeId && c.DeletedAt == null)
            .OrderBy(c => c.StartDate)
            .Select(c => c.StartDate)
            .FirstOrDefaultAsync(ct);

        if (startDate == default)
            return null;

        return DateOnly.FromDateTime(startDate);
    }

    private async Task<LeaveBalance?> GetExistingBalanceAsync(
        int companyId,
        int employeeId,
        int leaveTypeId,
        int year,
        int month,
        CancellationToken ct)
    {
        return await _db.LeaveBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.CompanyId == companyId &&
                b.EmployeeId == employeeId &&
                b.LeaveTypeId == leaveTypeId &&
                b.Year == year &&
                b.Month == month &&
                b.DeletedAt == null, ct);
    }

    private async Task<LeaveBalance> GetOrCreateBalanceAsync(
        int companyId,
        int employeeId,
        int leaveTypeId,
        int year,
        int month,
        int userId,
        CancellationToken ct,
        decimal openingDaysForCreate = 0m)
    {
        var balance = await _db.LeaveBalances
            .FirstOrDefaultAsync(b =>
                b.CompanyId == companyId &&
                b.EmployeeId == employeeId &&
                b.LeaveTypeId == leaveTypeId &&
                b.Year == year &&
                b.Month == month &&
                b.DeletedAt == null, ct);

        if (balance != null)
            return balance;

        balance = new LeaveBalance
        {
            CompanyId = companyId,
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            Month = month,
            OpeningDays = openingDaysForCreate < 0m ? 0m : openingDaysForCreate,
            AccruedDays = 0,
            UsedDays = 0,
            CarryInDays = 0,
            CarryOutDays = 0,
            ClosingDays = openingDaysForCreate < 0m ? 0m : openingDaysForCreate,
            LastRecalculatedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId
        };
        balance.CarryoverExpiresOn = balance.GetBalanceExpiresOn();

        _db.LeaveBalances.Add(balance);
        await _db.SaveChangesAsync(ct);

        return balance;
    }

    private async Task<decimal> ComputeUsedDaysAsync(
        int companyId,
        int employeeId,
        int leaveTypeId,
        int year,
        int month,
        CancellationToken ct)
    {
        return await _db.LeaveRequests
            .AsNoTracking()
            .Where(lr => lr.DeletedAt == null)
            .Where(lr => lr.CompanyId == companyId
                         && lr.EmployeeId == employeeId
                         && lr.LeaveTypeId == leaveTypeId)
            // Les congés annulés / renoncés ne doivent jamais être déduits.
            .Where(lr => lr.Status == LeaveRequestStatus.Approved)
            .Where(lr => !lr.IsRenounced)
            .Where(lr => lr.StartDate.Year == year && lr.StartDate.Month == month)
            .Where(lr => lr.LegalRuleId == null)
            .SumAsync(lr => lr.WorkingDaysDeducted, ct);
    }

    private static decimal ComputeAccruedDaysForMonth(
        LeaveTypePolicy policy,
        DateOnly contractStart,
        int year,
        int month)
    {
        if (policy.AccrualMethod == LeaveAccrualMethod.None)
            return 0m;

        var monthEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        if (contractStart > monthEnd)
            return 0m;

        if (policy.AccrualMethod == LeaveAccrualMethod.Monthly)
        {
            var accrued = policy.DaysPerMonthAdult;

            if (month == 1 && policy.BonusDaysPerYearAfter5Years > 0m && HasAtLeast5YearsSeniority(contractStart, monthEnd))
                accrued += policy.BonusDaysPerYearAfter5Years;

            return accrued;
        }

        return 0m;
    }

    private static bool HasAtLeast5YearsSeniority(DateOnly contractStart, DateOnly asOfDate)
    {
        var fiveYearsLater = contractStart.AddYears(5);
        return asOfDate >= fiveYearsLater;
    }

    private static decimal ComputeClosingDays(LeaveBalance b) =>
        b.OpeningDays + b.AccruedDays + b.CarryInDays - b.UsedDays - b.CarryOutDays;
}
