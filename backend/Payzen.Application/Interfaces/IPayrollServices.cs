using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;

namespace Payzen.Application.Interfaces;

/// <summary>
/// Orchestration du calcul de paie : prépare les données employé,
/// invoque PayrollCalculationEngine, persiste PayrollResult.
/// Implémenté en Phase 3 par PayrollService (AppDbContext + moteur).
/// </summary>
public interface IPayrollService
{
    /// <summary>Calcule et persiste la paie d'un employé pour un mois donné.</summary>
    Task<ServiceResult<PayrollResultReadDto>> CalculateAsync(PayrollSimulateRequestDto dto, int userId, CancellationToken ct = default);

    /// <summary>Calcule en mémoire sans persister (simulation / preview avant validation).</summary>
    Task<ServiceResult<PayrollResultReadDto>> SimulateAsync(PayrollSimulateRequestDto dto, CancellationToken ct = default);

    /// <summary>Calcule la paie de tous les employés actifs d'une société pour un mois.</summary>
    Task<ServiceResult<IEnumerable<PayrollResultReadDto>>> BatchCalculateAsync(PayrollBatchRequestDto dto, int userId, CancellationToken ct = default);

    /// <summary>Liste bulletins (GET api/payroll/results) — forme attendue par le frontend.</summary>
    Task<ServiceResult<PayrollBulletinResultsResponseDto>> GetResultsAsync(int? companyId, int month, int year, int? payHalf, string? statusFilter, CancellationToken ct = default);

    Task<ServiceResult<object>> GetStatsAsync(int companyId, int year, int month, CancellationToken ct = default);
    Task<ServiceResult<PayrollResultReadDto>> GetResultByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<PayrollBulletinDetailDto>> GetBulletinDetailAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<PayrollResultReadDto>> RecalculateForEmployeeAsync(int employeeId, int month, int year, int? payHalf, int userId, CancellationToken ct = default);
    Task<ServiceResult> DeleteResultAsync(int id, int deletedBy, CancellationToken ct = default);
}