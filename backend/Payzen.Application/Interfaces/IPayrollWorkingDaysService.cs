namespace Payzen.Application.Interfaces;

/// <summary>
/// Utilitaire de calcul des jours à déduire/déclarer pour la paie.
/// </summary>
public interface IPayrollWorkingDaysService
{
    Task<int> CalculateWorkingDaysAsync(
        int employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default
    );
}
