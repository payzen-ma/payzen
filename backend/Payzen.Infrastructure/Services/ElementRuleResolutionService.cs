using Microsoft.EntityFrameworkCore;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Payroll.Referentiel;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services;

/// <summary>
/// Résolution des règles d'éléments et paramètres légaux pour le moteur de paie.
/// Migration exacte de ElementRuleResolutionService du monolithe.
/// </summary>
public class ElementRuleResolutionService : IElementRuleResolutionService
{
    private readonly AppDbContext _db;
    public ElementRuleResolutionService(AppDbContext db) => _db = db;

    public async Task<decimal?> GetParameterValueEffectiveAtAsync(
        string code, DateOnly asOfDate, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return await _db.LegalParameters
            .AsNoTracking()
            .Where(p => p.Code.ToLower() == code.Trim().ToLower()
                     && p.DeletedAt == null
                     && p.EffectiveFrom <= asOfDate
                     && (p.EffectiveTo == null || p.EffectiveTo >= asOfDate))
            .OrderByDescending(p => p.EffectiveFrom)
            .Select(p => (decimal?)p.Value)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetParameterValuesEffectiveAtAsync(
        DateOnly asOfDate, CancellationToken ct = default)
    {
        var parameters = await _db.LegalParameters
            .AsNoTracking()
            .Where(p => p.DeletedAt == null
                     && p.EffectiveFrom <= asOfDate
                     && (p.EffectiveTo == null || p.EffectiveTo >= asOfDate))
            .ToListAsync(ct);

        return parameters
            .GroupBy(p => p.Code)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(p => p.EffectiveFrom).First().Value);
    }

    public async Task<IReadOnlyList<ElementRule>> GetRulesForElementAuthorityEffectiveAtAsync(
        int elementId, int authorityId, DateOnly asOfDate, CancellationToken ct = default)
    {
        return await _db.ElementRules
            .AsNoTracking()
            .Include(r => r.Cap)
            .Include(r => r.Formula!).ThenInclude(f => f.Parameter)
            .Include(r => r.Percentage)
            .Include(r => r.DualCap)
            .Include(r => r.Tiers)
            .Include(r => r.Variants)
            .Where(r => r.ElementId == elementId
                     && r.AuthorityId == authorityId
                     && r.DeletedAt == null
                     && r.EffectiveFrom <= asOfDate
                     && (r.EffectiveTo == null || r.EffectiveTo >= asOfDate))
            .OrderByDescending(r => r.EffectiveFrom)
            .ToListAsync(ct);
    }

    public async Task<int?> GetAuthorityIdByCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return await _db.Authorities
            .AsNoTracking()
            .Where(a => a.Code.ToLower() == code.Trim().ToLower() && a.DeletedAt == null)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(ct);
    }

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

        decimal GetBaseRef(BaseReference br) => br switch
        {
            BaseReference.BASE_SALARY  => baseSalary  ?? 0,
            BaseReference.GROSS_SALARY => grossSalary ?? 0,
            BaseReference.SBI          => sbi         ?? 0,
            _                          => baseSalary  ?? 0
        };

        decimal CapToMonthly(decimal amount, CapUnit unit) => unit switch
        {
            CapUnit.PER_MONTH => amount,
            CapUnit.PER_DAY   => amount * workingDaysPerMonth,
            CapUnit.PER_YEAR  => amount / 12,
            _                 => amount
        };

        switch (rule.ExemptionType)
        {
            case ExemptionType.FULLY_EXEMPT:
                return lineAmount;

            case ExemptionType.FULLY_SUBJECT:
                return 0;

            case ExemptionType.CAPPED:
                if (rule.Cap == null) return 0;
                return Math.Min(lineAmount, CapToMonthly(rule.Cap.CapAmount, rule.Cap.CapUnit));

            case ExemptionType.FORMULA:
                var fv = ResolveFormulaCap(rule, paramValuesByCode);
                if (fv == null) return 0;
                var fMonthly = rule.Formula != null ? CapToMonthly(fv.Value, rule.Formula.ResultUnit) : fv.Value;
                return Math.Min(lineAmount, fMonthly);

            case ExemptionType.FORMULA_CAPPED:
                var rfv = ResolveFormulaCap(rule, paramValuesByCode);
                if (rfv == null) return 0;
                var fcMonthly = rule.Formula != null ? CapToMonthly(rfv.Value, rule.Formula.ResultUnit) : rfv.Value;
                decimal? fixedCap = rule.Cap != null ? CapToMonthly(rule.Cap.CapAmount, rule.Cap.CapUnit) : null;
                var effCap = fixedCap.HasValue ? Math.Min(fcMonthly, fixedCap.Value) : fcMonthly;
                return Math.Min(lineAmount, effCap);

            case ExemptionType.PERCENTAGE:
                if (rule.Percentage == null) return 0;
                var pctExempt = GetBaseRef(rule.Percentage.BaseReference) * (rule.Percentage.Percentage / 100m);
                return Math.Min(lineAmount, pctExempt);

            case ExemptionType.PERCENTAGE_CAPPED:
                if (rule.Percentage == null) return 0;
                var pctCExempt = GetBaseRef(rule.Percentage.BaseReference) * (rule.Percentage.Percentage / 100m);
                if (rule.Cap != null)
                    pctCExempt = Math.Min(pctCExempt, CapToMonthly(rule.Cap.CapAmount, rule.Cap.CapUnit));
                return Math.Min(lineAmount, pctCExempt);

            case ExemptionType.DUAL_CAP:
                if (rule.DualCap == null) return 0;
                var dualBase = GetBaseRef(rule.DualCap.BaseReference);
                int periodsInUnit = rule.DualCap.FixedCapUnit switch
                {
                    CapUnit.PER_DAY   => workingDaysPerMonth,
                    CapUnit.PER_MONTH => 1,
                    CapUnit.PER_YEAR  => 1,
                    _                 => 1
                };
                var effDualCap = rule.DualCap.CalculateEffectiveCap(dualBase, periodsInUnit);
                if (rule.DualCap.FixedCapUnit == CapUnit.PER_YEAR) effDualCap /= 12;
                return Math.Min(lineAmount, effDualCap);

            case ExemptionType.TIERED:
                return ComputeTieredExempt(rule, lineAmount);

            default:
                return 0;
        }
    }

    private static decimal? ResolveFormulaCap(
        ElementRule rule, IReadOnlyDictionary<string, decimal> paramValuesByCode)
    {
        if (rule.Formula == null) return null;
        var code = rule.Formula.Parameter?.Code;
        if (!string.IsNullOrEmpty(code) && paramValuesByCode.TryGetValue(code, out var val))
            return val * rule.Formula.Multiplier;
        return rule.Formula.CalculateCurrentCap();
    }

    private static decimal ComputeTieredExempt(ElementRule rule, decimal lineAmount)
    {
        if (rule.Tiers == null || !rule.Tiers.Any()) return 0;
        decimal exempt = 0, remaining = lineAmount;
        foreach (var tier in rule.Tiers.OrderBy(t => t.TierOrder))
        {
            var segmentEnd = Math.Min(tier.ToAmount ?? lineAmount, lineAmount);
            if (segmentEnd <= tier.FromAmount || remaining <= 0) continue;
            var inTier = Math.Min(remaining, segmentEnd - tier.FromAmount);
            if (inTier <= 0) continue;
            exempt    += inTier * (tier.ExemptPercent / 100m);
            remaining -= inTier;
            if (remaining <= 0) break;
        }
        return Math.Min(lineAmount, exempt);
    }
}
