// Payzen.Application/Interfaces/IPayrollTaxSnapshotService.cs

using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;
using Payzen.Domain.Entities.Payroll;

namespace Payzen.Application.Interfaces;

public interface IPayrollTaxSnapshotService
{
    /// <summary>
    /// Construit et sauvegarde le snapshot cumulé après calcul d'un bulletin.
    /// Appelé par PayrollService juste après la sauvegarde du PayrollResult.
    /// </summary>
    Task<ServiceResult> BuildAndSaveAsync(
        PayrollResult payrollResult,
        CancellationToken ct = default);

    /// <summary>
    /// Retourne tous les snapshots d'une année pour le dashboard IR.
    /// GET /api/payroll/{employeeId}/tax-summary?year=2026
    /// </summary>
    Task<ServiceResult<List<PayrollTaxSnapshotDto>>> GetYearSummaryAsync(
        int employeeId,
        int companyId,
        int year,
        CancellationToken ct = default);

    /// <summary>
    /// Retourne le snapshot d'un mois précis.
    /// Utilisé en interne pour charger le cumul M-1.
    /// </summary>
    Task<ServiceResult<PayrollTaxSnapshot?>> GetByMonthAsync(
        int employeeId,
        int companyId,
        int month,
        int year,
        CancellationToken ct = default);
}
