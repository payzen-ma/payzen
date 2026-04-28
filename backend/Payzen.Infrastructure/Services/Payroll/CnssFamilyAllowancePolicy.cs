using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Payroll;

namespace Payzen.Infrastructure.Services.Payroll;

public sealed class CnssFamilyAllowancePolicy : ICnssFamilyAllowancePolicy
{
    private const int MaxEligibleChildren = 6;
    private const int FirstTrancheChildren = 3;
    private const long AfCentimesFirstTranche = 30_000L; // 300 DH
    private const long AfCentimesSecondTranche = 10_000L; // 100 DH
    private const long SmigMonthlyCentimes = 342_272L;
    private const decimal MinimumSalaryRatio = 0.60m;

    public int ResolveDependentChildren(PayrollResult payroll)
    {
        return payroll.Employee?.Children?.Count(c => c.IsDependent) ?? 0;
    }

    public long ComputeFamilyAllowanceToPayCentimes(PayrollResult payroll, int dependentChildren)
    {
        if (!IsLikelyEligible(payroll))
            return 0L;

        var eligibleChildren = Math.Clamp(dependentChildren, 0, MaxEligibleChildren);
        var firstTranche = Math.Min(eligibleChildren, FirstTrancheChildren);
        var secondTranche = Math.Max(0, eligibleChildren - FirstTrancheChildren);
        return (firstTranche * AfCentimesFirstTranche) + (secondTranche * AfCentimesSecondTranche);
    }

    public long ComputeFamilyAllowanceToDeductCentimes(PayrollResult payroll)
    {
        // Placeholder métier: aucune source déduction AF exploitée actuellement.
        return 0L;
    }

    public string ResolveSituation(PayrollResult payroll)
    {
        var statusCode = payroll.Employee?.Status?.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var statusNameFr = payroll.Employee?.Status?.NameFr?.Trim().ToUpperInvariant() ?? string.Empty;
        var statusNameEn = payroll.Employee?.Status?.NameEn?.Trim().ToUpperInvariant() ?? string.Empty;

        if (statusCode is "SO" or "DE" or "IT" or "IL" or "AT" or "CS" or "MS" or "MP")
            return statusCode;

        if (statusCode.Contains("DECE") || statusNameFr.Contains("DECE") || statusNameEn.Contains("DEATH"))
            return "DE";

        if (
            statusCode.Contains("SORT")
            || statusCode.Contains("EXIT")
            || statusCode.Contains("TERM")
            || statusNameFr.Contains("SORT")
            || statusNameEn.Contains("TERMIN")
            || statusNameEn.Contains("EXIT")
        )
            return "SO";

        return string.Empty;
    }

    public long ComputeFamilyAllowanceToReverseCentimes(string situation, long afNetCentimes)
    {
        return situation is "SO" or "DE" ? afNetCentimes : 0L;
    }

    private static bool IsLikelyEligible(PayrollResult payroll)
    {
        var salaryCentimes = ToCentimes(payroll.TotalBrut ?? payroll.CnssBase ?? 0m);
        var minimumCentimes = (long)Math.Round(
            SmigMonthlyCentimes * MinimumSalaryRatio,
            0,
            MidpointRounding.AwayFromZero
        );

        // NOTE: La condition des 108 jours sur 6 mois n'est pas traçable de manière fiable
        // avec le modèle courant. Elle sera ajoutée dès qu'une source consolidée des jours
        // déclarés historiques sera disponible.
        return salaryCentimes >= minimumCentimes;
    }

    private static long ToCentimes(decimal amountMad)
    {
        return (long)Math.Round(amountMad * 100m, 0, MidpointRounding.AwayFromZero);
    }
}
