using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Leave;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Services.Leave
{
    /// <summary>
    /// Service centralisé pour:
    /// - Resolve policy (company puis global) avec EffectiveFrom/To
    /// - GetOrCreate LeaveBalance
    /// - Recalculate (Accrued/Used/Closing) "as-of date" (ex: request.StartDate)
    ///
    /// Objectif: supporter le cas où LeaveBalance est vide mais l'employé a déjà accumulé
    /// (ex: contrat commencé en mai 2025, demande en déc 2025).
    /// </summary>
    public class LeaveBalanceService
    {
        private readonly AppDbContext _db;

        public LeaveBalanceService(AppDbContext db)
        {
            _db = db;
        }
        
        private static void GetPreviousMonth(int year, int month, out int prevYear, out int prevMonth)
        {
            if (month == 1) { prevYear = year - 1; prevMonth = 12; }
            else { prevYear = year; prevMonth = month - 1; }
        }

        /// <param name="referenceDateForExpiry">Date de référence pour l'expiration : si le mois précédent a déjà expiré à cette date, son closing n'est pas reporté (perdu).</param>
        private async Task<decimal> ComputeCarryInDaysAsync(
            int companyId,
            int employeeId,
            int leaveTypeId,
            int year,
            int month,
            LeaveTypePolicy policy,
            int userId,
            DateOnly referenceDateForExpiry,
            CancellationToken ct)
        {
            GetPreviousMonth(year, month, out int prevYear, out int prevMonth);

            var contractStart = await GetEmployeeContractStartAsync(employeeId, ct);
            if (contractStart == null) return 0m;

            // Ne pas créer de soldes pour les mois avant le début du contrat.
            var contractYear = contractStart.Value.Year;
            var contractMonth = contractStart.Value.Month;
            if (prevYear < contractYear || (prevYear == contractYear && prevMonth < contractMonth))
                return 0m;

            // Recalculer récursivement le mois précédent (toute la chaîne depuis le début du contrat).
            var prevAsOf = new DateOnly(prevYear, prevMonth, 1);
            var recalc = await RecalculateAsync(companyId, employeeId, leaveTypeId, prevAsOf, userId, ct, referenceDateForExpiry);
            if (!recalc.Success || recalc.Balance == null)
                return 0m;

            // Si le mois précédent a déjà expiré à la date de référence, ne pas reporter son closing (ex. 01/2020 expire 31/01/2022 → au 01/02/2022 on ne reporte pas ses 1,5 j).
            if (recalc.Balance.CarryoverExpiresOn.HasValue && recalc.Balance.CarryoverExpiresOn.Value < referenceDateForExpiry)
                return 0m;

            // N'utiliser le report que si la politique l'autorise.
            if (!policy.AllowCarryover || policy.MaxCarryoverYears <= 0)
                return 0m;

            return recalc.Balance.ClosingDays < 0 ? 0m : recalc.Balance.ClosingDays;
        }
        // =========================================================
        // PUBLIC API
        // =========================================================

        /// <summary>
        /// Recalcule (et crée si besoin) le solde d'un employé pour un mois donné (Year, Month).
        /// </summary>
        /// <param name="referenceDateForExpiry">Si fourni, les mois expirés à cette date ne sont pas reportés (carry-in = 0). Sinon = asOfDate.</param>
        public async Task<LeaveBalanceRecalcResult> RecalculateAsync(
            int companyId,
            int employeeId,
            int leaveTypeId,
            DateOnly asOfDate,
            int userId,
            CancellationToken ct = default,
            DateOnly? referenceDateForExpiry = null)
        {
            var year = asOfDate.Year;
            var month = asOfDate.Month;
            var refDate = referenceDateForExpiry ?? asOfDate;

            var policy = await ResolvePolicyAsync(companyId, leaveTypeId, asOfDate, ct);
            if (policy == null)
                return LeaveBalanceRecalcResult.Fail("Aucune politique active trouvée pour ce type de congé (company/global)");

            var contractStart = await GetEmployeeContractStartAsync(employeeId, ct);
            if (contractStart == null)
                return LeaveBalanceRecalcResult.Fail("Aucun contrat actif trouvé pour l'employé");

            var balance = await GetOrCreateBalanceAsync(companyId, employeeId, leaveTypeId, year, month, userId, ct);

            balance.AccruedDays = ComputeAccruedDaysForMonth(policy, contractStart.Value, year, month);
            balance.UsedDays = await ComputeUsedDaysAsync(companyId, employeeId, leaveTypeId, year, month, ct);
            balance.CarryInDays = await ComputeCarryInDaysAsync(
                companyId, employeeId, leaveTypeId, year, month, policy, userId, refDate, ct);

            balance.CarryOutDays = 0m;
            balance.ClosingDays = ComputeClosingDays(balance);

            if (policy.AnnualCapDays > 0 && balance.AccruedDays > policy.AnnualCapDays)
            {
                balance.AccruedDays = policy.AnnualCapDays;
                balance.ClosingDays = ComputeClosingDays(balance);
            }

            // Plafond de report : au-delà de AnnualCapDays, l'excédent est "perdu" (CarryOutDays), le closing est plafonné.
            if (policy.AllowCarryover && policy.AnnualCapDays > 0 && balance.ClosingDays > policy.AnnualCapDays)
            {
                balance.CarryOutDays = balance.ClosingDays - policy.AnnualCapDays;
                balance.ClosingDays = ComputeClosingDays(balance); // Opening + Accrued + CarryIn - Used - CarryOut
            }

            balance.CarryoverExpiresOn = balance.GetBalanceExpiresOn(); // Pour audit : date d'expiration du report (exclu du calcul si dépassée)
            balance.LastRecalculatedAt = DateTimeOffset.UtcNow;
            balance.ModifiedAt = DateTimeOffset.UtcNow;
            balance.ModifiedBy = userId;

            await _db.SaveChangesAsync(ct);

            return LeaveBalanceRecalcResult.Ok(balance, policy);
        }

        /// <summary>
        /// Vérifie si une demande peut être soumise.
        /// Le solde disponible = somme des ClosingDays de tous les mois encore valides (non expirés après 2 ans).
        /// </summary>
        public async Task<BalanceCheckResult> CheckSufficientBalanceForSubmitAsync(
            int companyId,
            int employeeId,
            int leaveTypeId,
            DateOnly requestStartDate,
            decimal requestedWorkingDays,
            int userId,
            CancellationToken ct = default)
        {
            var policy = await ResolvePolicyAsync(companyId, leaveTypeId, requestStartDate, ct);
            if (policy == null)
                return BalanceCheckResult.Fail("Aucune politique active trouvée pour ce type de congé (company/global)");

            var balanceForMonth = await GetExistingBalanceAsync(companyId, employeeId, leaveTypeId, requestStartDate.Year, requestStartDate.Month, ct);
            if (balanceForMonth == null)
            {
                var recalc = await RecalculateAsync(companyId, employeeId, leaveTypeId, requestStartDate, userId, ct);
                if (!recalc.Success)
                    return BalanceCheckResult.Fail(recalc.ErrorMessage ?? "Erreur lors de la création du solde");
                balanceForMonth = recalc.Balance!;
            }

            if (!policy.RequiresBalance)
                return BalanceCheckResult.Ok(balanceForMonth, policy);

            if (policy.AllowNegativeBalance)
                return BalanceCheckResult.Ok(balanceForMonth, policy);

            var totalAvailable = await GetTotalNonExpiredClosingDaysAsync(companyId, employeeId, leaveTypeId, requestStartDate, ct);
            if (totalAvailable < requestedWorkingDays)
                return BalanceCheckResult.Fail("Solde de congés insuffisant", balanceForMonth, policy);

            return BalanceCheckResult.Ok(balanceForMonth, policy);
        }

        /// <summary>
        /// Récupère ou crée le solde pour un mois donné (pour déduction lors de l'approbation d'une demande).
        /// </summary>
        public async Task<LeaveBalance?> GetOrCreateBalanceForMonthAsync(
            int companyId,
            int employeeId,
            int leaveTypeId,
            int year,
            int month,
            int userId,
            CancellationToken ct = default)
        {
            return await GetOrCreateBalanceAsync(companyId, employeeId, leaveTypeId, year, month, userId, ct);
        }

        /// <summary>
        /// Retourne la somme des ClosingDays de tous les soldes encore valides (non expirés) à la date donnée.
        /// Un solde est considéré expiré si CarryoverExpiresOn &lt; asOfDate (exclu du calcul).
        /// </summary>
        public async Task<decimal> GetTotalNonExpiredClosingDaysAsync(
            int companyId,
            int employeeId,
            int leaveTypeId,
            DateOnly asOfDate,
            CancellationToken ct = default)
        {
            var list = await _db.LeaveBalances
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId && b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId && b.DeletedAt == null)
                // Exclure les soldes expirés : ne compter que ceux encore valides à asOfDate
                .Where(b => b.CarryoverExpiresOn != null && b.CarryoverExpiresOn.Value >= asOfDate)
                .ToListAsync(ct);

            return list.Sum(b => b.ClosingDays);
        }

        // =========================================================
        // RESOLVE POLICY
        // =========================================================

        private async Task<LeaveTypePolicy?> ResolvePolicyAsync(int companyId, int leaveTypeId, DateOnly asOfDate, CancellationToken ct)
        {
            // Company policy
            var companyPolicy = await _db.LeaveTypePolicies
                .AsNoTracking()
                .Where(p => p.DeletedAt == null && p.IsEnabled)
                .Where(p => p.LeaveTypeId == leaveTypeId && p.CompanyId == companyId)
                .Where(p => p.EffectiveFrom == null || p.EffectiveFrom <= asOfDate)
                .Where(p => p.EffectiveTo == null || p.EffectiveTo >= asOfDate)
                // si plusieurs versions, prendre la plus récente
                .OrderByDescending(p => p.EffectiveFrom ?? new DateOnly(1900, 1, 1))
                .FirstOrDefaultAsync(ct);

            if (companyPolicy != null)
                return companyPolicy;

            // Global policy fallback
            var globalPolicy = await _db.LeaveTypePolicies
                .AsNoTracking()
                .Where(p => p.DeletedAt == null && p.IsEnabled)
                .Where(p => p.LeaveTypeId == leaveTypeId && p.CompanyId == null)
                .Where(p => p.EffectiveFrom == null || p.EffectiveFrom <= asOfDate)
                .Where(p => p.EffectiveTo == null || p.EffectiveTo >= asOfDate)
                .OrderByDescending(p => p.EffectiveFrom ?? new DateOnly(1900, 1, 1))
                .FirstOrDefaultAsync(ct);

            return globalPolicy;
        }

        // =========================================================
        // GET CONTRACT START
        // =========================================================

        private async Task<DateOnly?> GetEmployeeContractStartAsync(int employeeId, CancellationToken ct)
        {
            // Choix: premier contrat (StartDate minimal) non supprimé
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

        // =========================================================
        // GET EXISTING BALANCE (lecture seule, pas de création)
        // =========================================================

        private async Task<LeaveBalance?> GetExistingBalanceAsync(
            int companyId,
            int employeeId,
            int leaveTypeId,
            int year,
            int month,
            CancellationToken ct)
        {
            return await _db.LeaveBalances
                .FirstOrDefaultAsync(b =>
                    b.CompanyId == companyId &&
                    b.EmployeeId == employeeId &&
                    b.LeaveTypeId == leaveTypeId &&
                    b.Year == year &&
                    b.Month == month &&
                    b.DeletedAt == null, ct);
        }

        // =========================================================
        // GET OR CREATE BALANCE
        // =========================================================

        private async Task<LeaveBalance> GetOrCreateBalanceAsync(
            int companyId,
            int employeeId,
            int leaveTypeId,
            int year,
            int month,
            int userId,
            CancellationToken ct)
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

                OpeningDays = 0,
                AccruedDays = 0,
                UsedDays = 0,
                CarryInDays = 0,
                CarryOutDays = 0,
                ClosingDays = 0,

                LastRecalculatedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };
            balance.CarryoverExpiresOn = balance.GetBalanceExpiresOn(); // Audit : date d'expiration du report

            _db.LeaveBalances.Add(balance);
            await _db.SaveChangesAsync(ct);

            return balance;
        }

        // =========================================================
        // USED DAYS
        // =========================================================

        /// <summary>
        /// Jours utilisés (consommant le solde) pour le mois.
        /// Les congés légaux (LeaveRequest.LegalRuleId renseigné, ex. mariage, décès, naissance)
        /// ne consomment pas le solde : ils ne sont pas déduits ici.
        /// Seuls les congés "non légaux" (sans LegalRuleId) sont déduits du solde.
        /// </summary>
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
                .Where(lr => lr.Status == LeaveRequestStatus.Approved)
                .Where(lr => !lr.IsRenounced)
                .Where(lr => lr.StartDate.Year == year && lr.StartDate.Month == month)
                .Where(lr => lr.LegalRuleId == null) // congés légaux (EventCaseCode MARRIAGE_EMPLOYEE, BIRTH, etc.) ne consomment pas le solde
                .SumAsync(lr => lr.WorkingDaysDeducted, ct);
        }

        // =========================================================
        // ACCRUAL (par mois)
        // =========================================================

        /// <summary>
        /// Jours acquis pour un mois donné. Le contrat doit couvrir ce mois.
        /// Bonus après 5 ans attribué en janvier si ancienneté atteinte.
        /// </summary>
        private static decimal ComputeAccruedDaysForMonth(
            LeaveTypePolicy policy,
            DateOnly contractStart,
            int year,
            int month)
        {
            if (policy.AccrualMethod == LeaveAccrualMethod.None)
                return 0m;

            var monthStart = new DateOnly(year, month, 1);
            var monthEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

            // Le contrat doit être actif pendant ce mois (au moins un jour)
            if (contractStart > monthEnd)
                return 0m;

            if (policy.AccrualMethod == LeaveAccrualMethod.Monthly)
            {
                var accrued = policy.DaysPerMonthAdult;

                // Bonus après 5 ans : attribué en janvier si ancienneté >= 5 ans à la fin de janvier
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

        // =========================================================
        // CLOSING
        // =========================================================

        private static decimal ComputeClosingDays(LeaveBalance b)
        {
            return b.OpeningDays + b.AccruedDays + b.CarryInDays - b.UsedDays - b.CarryOutDays;
        }
    }

    // =========================================================
    // RESULTS
    // =========================================================

    public sealed class LeaveBalanceRecalcResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public LeaveBalance? Balance { get; private set; }
        public LeaveTypePolicy? Policy { get; private set; }

        public static LeaveBalanceRecalcResult Ok(LeaveBalance balance, LeaveTypePolicy policy)
            => new LeaveBalanceRecalcResult { Success = true, Balance = balance, Policy = policy };

        public static LeaveBalanceRecalcResult Fail(string message)
            => new LeaveBalanceRecalcResult { Success = false, ErrorMessage = message };
    }

    public sealed class BalanceCheckResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public LeaveBalance? Balance { get; private set; }
        public LeaveTypePolicy? Policy { get; private set; }

        public static BalanceCheckResult Ok(LeaveBalance balance, LeaveTypePolicy policy)
            => new BalanceCheckResult { Success = true, Balance = balance, Policy = policy };

        public static BalanceCheckResult Fail(string message, LeaveBalance? balance = null, LeaveTypePolicy? policy = null)
            => new BalanceCheckResult { Success = false, ErrorMessage = message, Balance = balance, Policy = policy };
    }
}
