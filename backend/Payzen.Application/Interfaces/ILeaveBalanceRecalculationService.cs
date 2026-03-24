namespace Payzen.Application.Interfaces;

/// <summary>Recalcul mensuel des soldes (acquisition, consommation, report) aligné sur l'ancien LeaveBalanceService.</summary>
public interface ILeaveBalanceRecalculationService
{
    /// <summary>Recalcule chaque mois du début du contrat jusqu'à (endYear, endMonth) inclus — solde cohérent sur toute la chaîne.</summary>
    Task<LeaveBalanceMonthRecalcResult> RecalculateRangeThroughMonthAsync(
        int companyId,
        int employeeId,
        int leaveTypeId,
        int endYear,
        int endMonth,
        int userId,
        CancellationToken ct = default,
        DateOnly? referenceDateForExpiry = null);

    /// <summary>Équivalent à <see cref="RecalculateRangeThroughMonthAsync"/> pour le mois de <paramref name="asOfDate"/> uniquement (même chaîne complète).</summary>
    Task<LeaveBalanceMonthRecalcResult> RecalculateAsync(
        int companyId,
        int employeeId,
        int leaveTypeId,
        DateOnly asOfDate,
        int userId,
        CancellationToken ct = default,
        DateOnly? referenceDateForExpiry = null);
}

public sealed class LeaveBalanceMonthRecalcResult
{
    public bool Success { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static LeaveBalanceMonthRecalcResult Ok() => new() { Success = true };

    public static LeaveBalanceMonthRecalcResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
