using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.Services
{
    /// <summary>
    /// Resolves legal parameters and element rules for payroll; computes exempt amounts per line.
    /// </summary>
    public class ElementRuleResolutionService : IElementRuleResolutionService
    {
        private readonly AppDbContext _db;

        public ElementRuleResolutionService(AppDbContext db)
        {
            _db = db;
        }

        /// <inheritdoc />
        public async Task<decimal?> GetParameterValueEffectiveAtAsync(string code, DateOnly asOfDate, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var value = await _db.LegalParameters
                .AsNoTracking()
                .Where(p =>
                    p.Code.ToLower() == code.Trim().ToLower() &&
                    p.DeletedAt == null &&
                    p.EffectiveFrom <= asOfDate &&
                    (p.EffectiveTo == null || p.EffectiveTo >= asOfDate))
                .OrderByDescending(p => p.EffectiveFrom)
                .Select(p => (decimal?)p.Value)
                .FirstOrDefaultAsync(cancellationToken);
            return value;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, decimal>> GetParameterValuesEffectiveAtAsync(DateOnly asOfDate, CancellationToken cancellationToken = default)
        {
            var parameters = await _db.LegalParameters
                .AsNoTracking()
                .Where(p =>
                    p.DeletedAt == null &&
                    p.EffectiveFrom <= asOfDate &&
                    (p.EffectiveTo == null || p.EffectiveTo >= asOfDate))
                .ToListAsync(cancellationToken);
            return parameters
                .GroupBy(p => p.Code)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFrom).First().Value);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ElementRule>> GetRulesForElementAuthorityEffectiveAtAsync(
            int elementId,
            int authorityId,
            DateOnly asOfDate,
            CancellationToken cancellationToken = default)
        {
            var rules = await _db.ElementRules
                .AsNoTracking()
                .Include(r => r.Cap)
                .Include(r => r.Formula!)
                    .ThenInclude(f => f.Parameter)
                .Include(r => r.Percentage)
                .Include(r => r.DualCap)
                .Include(r => r.Tiers)
                .Include(r => r.Variants)
                .Where(r =>
                    r.ElementId == elementId &&
                    r.AuthorityId == authorityId &&
                    r.DeletedAt == null &&
                    r.EffectiveFrom <= asOfDate &&
                    (r.EffectiveTo == null || r.EffectiveTo >= asOfDate))
                .OrderByDescending(r => r.EffectiveFrom)
                .ToListAsync(cancellationToken);
            return rules;
        }

        /// <inheritdoc />
        public async Task<int?> GetAuthorityIdByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var id = await _db.Authorities
                .AsNoTracking()
                .Where(a => a.Code.ToLower() == code.Trim().ToLower() && a.DeletedAt == null)
                .Select(a => (int?)a.Id)
                .FirstOrDefaultAsync(cancellationToken);
            return id;
        }

        /// <inheritdoc />
        public decimal ComputeExemptAmount(
            ElementRule rule,
            decimal lineAmount,
            decimal? baseSalary,
            decimal? grossSalary,
            decimal? sbi,
            IReadOnlyDictionary<string, decimal>? paramValuesByCode,
            int workingDaysPerMonth = 26)
        {
            paramValuesByCode ??= new Dictionary<string, decimal>();

            decimal GetBaseReference(BaseReference br)
            {
                return br switch
                {
                    BaseReference.BASE_SALARY => baseSalary ?? 0,
                    BaseReference.GROSS_SALARY => grossSalary ?? 0,
                    BaseReference.SBI => sbi ?? 0,
                    _ => baseSalary ?? 0
                };
            }

            decimal CapToMonthly(decimal amount, CapUnit unit)
            {
                return unit switch
                {
                    CapUnit.PER_MONTH => amount,
                    CapUnit.PER_DAY => amount * workingDaysPerMonth,
                    CapUnit.PER_YEAR => amount / 12,
                    _ => amount
                };
            }

            switch (rule.ExemptionType)
            {
                case ExemptionType.FULLY_EXEMPT:
                    return lineAmount;

                case ExemptionType.FULLY_SUBJECT:
                    return 0;

                case ExemptionType.CAPPED:
                    if (rule.Cap == null) return 0;
                    var capMonthly = CapToMonthly(rule.Cap.CapAmount, rule.Cap.CapUnit);
                    return Math.Min(lineAmount, capMonthly);

                case ExemptionType.FORMULA:
                    var formulaVal = ResolveFormulaCap(rule, paramValuesByCode);
                    if (formulaVal == null) return 0;
                    var formulaMonthly = rule.Formula != null
                        ? CapToMonthly(formulaVal.Value, rule.Formula.ResultUnit)
                        : formulaVal.Value;
                    return Math.Min(lineAmount, formulaMonthly);

                case ExemptionType.FORMULA_CAPPED:
                    var rawFormula = ResolveFormulaCap(rule, paramValuesByCode);
                    if (rawFormula == null) return 0;
                    var formulaCapVal = rawFormula != null && rule.Formula != null
                        ? CapToMonthly(rawFormula.Value, rule.Formula.ResultUnit)
                        : rawFormula;
                    decimal? fixedCap = rule.Cap != null ? CapToMonthly(rule.Cap.CapAmount, rule.Cap.CapUnit) : null;
                    var effectiveCap = fixedCap.HasValue ? Math.Min(formulaCapVal!.Value, fixedCap.Value) : formulaCapVal!.Value;
                    return Math.Min(lineAmount, effectiveCap);

                case ExemptionType.PERCENTAGE:
                    if (rule.Percentage == null) return 0;
                    var baseVal = GetBaseReference(rule.Percentage.BaseReference);
                    // RulePercentage.Percentage is stored as human % (35 = 35%), divide by 100
                    var exemptPct = baseVal * (rule.Percentage.Percentage / 100m);
                    return Math.Min(lineAmount, exemptPct);

                case ExemptionType.PERCENTAGE_CAPPED:
                    if (rule.Percentage == null) return 0;
                    var baseValC = GetBaseReference(rule.Percentage.BaseReference);
                    // RulePercentage.Percentage is stored as human % (35 = 35%), divide by 100
                    var exemptPctC = baseValC * (rule.Percentage.Percentage / 100m);
                    if (rule.Cap != null)
                    {
                        var capC = CapToMonthly(rule.Cap.CapAmount, rule.Cap.CapUnit);
                        exemptPctC = Math.Min(exemptPctC, capC);
                    }
                    return Math.Min(lineAmount, exemptPctC);

                case ExemptionType.DUAL_CAP:
                    if (rule.DualCap == null) return 0;
                    var dualBase = GetBaseReference(rule.DualCap.BaseReference);
                    int periodsInUnit = rule.DualCap.FixedCapUnit switch
                    {
                        CapUnit.PER_DAY => workingDaysPerMonth,
                        CapUnit.PER_MONTH => 1,
                        CapUnit.PER_YEAR => 1, // result will be yearly; we convert to monthly below
                        _ => 1
                    };
                    var effectiveDualCap = rule.DualCap.CalculateEffectiveCap(dualBase, periodsInUnit);
                    if (rule.DualCap.FixedCapUnit == CapUnit.PER_YEAR)
                        effectiveDualCap /= 12;
                    return Math.Min(lineAmount, effectiveDualCap);

                case ExemptionType.TIERED:
                    return ComputeTieredExempt(rule, lineAmount);

                default:
                    return 0;
            }
        }

        private static decimal? ResolveFormulaCap(ElementRule rule, IReadOnlyDictionary<string, decimal> paramValuesByCode)
        {
            if (rule.Formula == null) return null;
            var code = rule.Formula.Parameter?.Code;
            if (!string.IsNullOrEmpty(code) && paramValuesByCode.TryGetValue(code, out var value))
                return value * rule.Formula.Multiplier;
            return rule.Formula.CalculateCurrentCap();
        }

        private static decimal ComputeTieredExempt(ElementRule rule, decimal lineAmount)
        {
            if (rule.Tiers == null || !rule.Tiers.Any()) return 0;
            var ordered = rule.Tiers.OrderBy(t => t.TierOrder).ToList();
            decimal exempt = 0;
            decimal remaining = lineAmount;
            foreach (var tier in ordered)
            {
                var from = tier.FromAmount;
                var to = tier.ToAmount ?? lineAmount;
                var segmentEnd = Math.Min(to, lineAmount);
                if (segmentEnd <= from || remaining <= 0) continue;
                var amountInTier = Math.Min(remaining, segmentEnd - from);
                if (amountInTier <= 0) continue;
                // ExemptPercent is stored as human % (100 = 100%), divide by 100
                exempt += amountInTier * (tier.ExemptPercent / 100m);
                remaining -= amountInTier;
                if (remaining <= 0) break;
            }
            return Math.Min(lineAmount, exempt);
        }
    }
}
