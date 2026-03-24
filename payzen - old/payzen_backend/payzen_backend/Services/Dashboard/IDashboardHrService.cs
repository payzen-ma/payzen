using payzen_backend.Models.Dashboard.Dtos;

namespace payzen_backend.Services.Dashboard
{
    public interface IDashboardHrService
    {
        Task<DashboardHrRawDto> GetHrDashboardRawAsync(int? companyId, string? month, CancellationToken cancellationToken = default);
        Task<DashboardHrDto> GetHrDashboardAsync(int? companyId, string? month, CancellationToken cancellationToken = default);
        Task<DashboardHrVueGlobaleDto> GetVueGlobaleAsync(int? companyId, string? month, CancellationToken cancellationToken = default);
        Task<DashboardHrMouvementsDto> GetMouvementsRhAsync(int? companyId, string? month, CancellationToken cancellationToken = default);
        Task<DashboardHrMasseSalarialeDto> GetMasseSalarialeAsync(int? companyId, string? month, CancellationToken cancellationToken = default);
        Task<DashboardHrPariteDiversiteDto> GetPariteDiversiteAsync(int? companyId, string? month, CancellationToken cancellationToken = default);
        Task<DashboardHrConformiteSocialeDto> GetConformiteSocialeAsync(int? companyId, string? month, CancellationToken cancellationToken = default);
    }
}
