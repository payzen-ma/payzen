using Payzen.Application.Common;
using Payzen.Application.DTOs.Dashboard;

namespace Payzen.Application.Interfaces;

/// <summary>
/// Tableaux de bord RH.
/// Miroir exact de IDashboardHrService du source.
/// Implémenté en Phase 3 par DashboardHrService (AppDbContext).
/// </summary>
public interface IDashboardService
{
    // ── Dashboard HR (par société / par mois) ────────────────
    Task<DashboardHrRawDto> GetHrDashboardRawAsync(int? companyId, string? month, CancellationToken ct = default);
    Task<DashboardHrDto> GetHrDashboardAsync(int? companyId, string? month, CancellationToken ct = default);
    Task<DashboardHrVueGlobaleDto> GetVueGlobaleAsync(int? companyId, string? month, CancellationToken ct = default);
    Task<DashboardHrMouvementsDto> GetMouvementsRhAsync(int? companyId, string? month, CancellationToken ct = default);
    Task<DashboardHrMasseSalarialeDto> GetMasseSalarialeAsync(int? companyId, string? month, CancellationToken ct = default);
    Task<DashboardHrPariteDiversiteDto> GetPariteDiversiteAsync(int? companyId, string? month, CancellationToken ct = default);
    Task<DashboardHrConformiteSocialeDto> GetConformiteSocialeAsync(int? companyId, string? month, CancellationToken ct = default);

    // ── Backoffice / Expert / Dashboard général / Dashboard Employee ──────────────
    Task<ServiceResult<DashboardSummaryDto>> GetBackofficeSummaryAsync(CancellationToken ct = default);
    Task<ServiceResult<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default);
    Task<ServiceResult<object>> GetEmployeesSnapshotAsync(int? companyId, CancellationToken ct = default);
    Task<ServiceResult<ExpertDashboardDto>> GetExpertDashboardAsync(int expertCompanyId, CancellationToken ct = default);
    Task<ServiceResult<DashboardResponseDto>> GetEmployeesDashboardAsync(int userId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeDashboardDataDto>> GetEmployeeDashboardAsync(int userId, CancellationToken ct = default);
    Task<ServiceResult<CeoDashboardDto>> GetCeoDashboardDataAsync(
        int userId,
        string? parity = null,
        string? fromMonth = null,
        string? toMonth = null,
        CancellationToken ct = default);
}