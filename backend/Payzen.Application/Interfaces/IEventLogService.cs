using Payzen.Application.Common;

namespace Payzen.Application.Interfaces;

/// <summary>Journal d'événements pour Company, Employee et Leave.</summary>
public interface ICompanyEventLogService
{
    Task LogEventAsync(
        int companyId,
        string eventName,
        string? oldValue,
        int? oldValueId,
        string? newValue,
        int? newValueId,
        int createdBy,
        CancellationToken ct = default
    );
    Task LogSimpleEventAsync(
        int companyId,
        string eventName,
        string? oldValue,
        string? newValue,
        int createdBy,
        CancellationToken ct = default
    );
}

public interface IEmployeeEventLogService
{
    Task LogEventAsync(
        int employeeId,
        string eventName,
        string? oldValue,
        int? oldValueId,
        string? newValue,
        int? newValueId,
        int createdBy,
        CancellationToken ct = default
    );
    Task LogSimpleEventAsync(
        int employeeId,
        string eventName,
        string? oldValue,
        string? newValue,
        int createdBy,
        CancellationToken ct = default
    );
}

public interface ILeaveEventLogService
{
    Task LogEventAsync(
        int companyId,
        int? employeeId,
        int? leaveRequestId,
        string eventName,
        string? oldValue,
        string? newValue,
        int createdBy,
        CancellationToken ct = default
    );
    Task LogLeaveRequestEventAsync(
        int companyId,
        int? employeeId,
        int leaveRequestId,
        string eventName,
        string? oldValue,
        string? newValue,
        int createdBy,
        CancellationToken ct = default
    );
}

/// <summary>Calcul des jours ouvrables selon calendrier entreprise et jours fériés.</summary>
public interface IWorkingDaysCalculator
{
    Task<decimal> CalculateWorkingDaysAsync(
        int companyId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default
    );
}

/// <summary>Résolution des règles d'éléments et paramètres légaux pour le moteur de paie.</summary>
public interface IElementRuleResolutionService
{
    Task<decimal?> GetParameterValueEffectiveAtAsync(string code, DateOnly asOfDate, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, decimal>> GetParameterValuesEffectiveAtAsync(
        DateOnly asOfDate,
        CancellationToken ct = default
    );
    Task<
        IReadOnlyList<Payzen.Domain.Entities.Payroll.Referentiel.ElementRule>
    > GetRulesForElementAuthorityEffectiveAtAsync(
        int elementId,
        int authorityId,
        DateOnly asOfDate,
        CancellationToken ct = default
    );
    Task<int?> GetAuthorityIdByCodeAsync(string code, CancellationToken ct = default);
    decimal ComputeExemptAmount(
        Payzen.Domain.Entities.Payroll.Referentiel.ElementRule rule,
        decimal lineAmount,
        decimal? baseSalary,
        decimal? grossSalary,
        decimal? sbi,
        IReadOnlyDictionary<string, decimal>? paramValuesByCode,
        int workingDaysPerMonth = 26
    );
}

/// <summary>Seed des données par défaut lors de la création d'une entreprise.</summary>
public interface ICompanyDefaultsSeeder
{
    Task SeedDefaultsAsync(int companyId, int userId, CancellationToken ct = default);
}
