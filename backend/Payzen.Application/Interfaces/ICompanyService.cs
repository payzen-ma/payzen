using Microsoft.AspNetCore.Http;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Company;

namespace Payzen.Application.Interfaces;

/// <summary>
/// CRUD entreprises + sous-ressources (départements, postes, types de contrat, calendrier, jours fériés, documents).
/// Implémenté en Phase 3 par CompanyService (AppDbContext + ICompanyOnboardingService).
/// </summary>
public interface ICompanyService
{
    // ── Company ──────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<CompanyListDto>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<CompanyReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<CompanyListDto>>> GetByCityAsync(int cityId, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<CompanyListDto>>> GetByCountryAsync(int countryId, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<CompanyListDto>>> SearchAsync(string searchTerm, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<CompanyListDto>>> GetManagedByAsync(int expertCompanyId, CancellationToken ct = default);
    Task<ServiceResult<CompanyFormDataDto>> GetFormDataAsync(CancellationToken ct = default);
    Task<bool> CountryExistsAsync(int countryId, CancellationToken ct = default);
    Task<bool> CityExistsForCountryAsync(int cityId, int countryId, CancellationToken ct = default);
    Task<ServiceResult<CompanyCreateResponseDto>> CreateAsync(CompanyCreateDto dto, int createdBy, CancellationToken ct = default, bool sendInvitation = true, int? existingAdminUserId = null, bool createAdminAccount = true);
    Task<ServiceResult<CompanyCreateResponseDto>> CreateByExpertAsync(CompanyCreateByExpertDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<CompanyReadDto>> PatchAsync(int id, CompanyUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<CompanyHistoryDto>>> GetHistoryAsync(int companyId, CancellationToken ct = default);

    // ── Département ──────────────────────────────────────────
    Task<ServiceResult<IEnumerable<DepartementReadDto>>> GetAllDepartementsAsync(CancellationToken ct = default);
    Task<ServiceResult<DepartementReadDto>> GetDepartementByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<DepartementReadDto>>> GetDepartementsAsync(int companyId, CancellationToken ct = default);
    Task<ServiceResult<DepartementReadDto>> CreateDepartementAsync(DepartementCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<DepartementReadDto>> UpdateDepartementAsync(int id, DepartementUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteDepartementAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── JobPosition ──────────────────────────────────────────
    Task<ServiceResult<IEnumerable<JobPositionReadDto>>> GetAllJobPositionsAsync(CancellationToken ct = default);
    Task<ServiceResult<JobPositionReadDto>> GetJobPositionByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<JobPositionReadDto>>> GetJobPositionsAsync(int companyId, CancellationToken ct = default);
    Task<ServiceResult<JobPositionReadDto>> CreateJobPositionAsync(JobPositionCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<JobPositionReadDto>> UpdateJobPositionAsync(int id, JobPositionUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteJobPositionAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── ContractType ─────────────────────────────────────────
    Task<ServiceResult<IEnumerable<ContractTypeReadDto>>> GetContractTypesAsync(int companyId, CancellationToken ct = default);
    Task<ServiceResult<ContractTypeReadDto>> GetContractTypeByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<ContractTypeReadDto>> CreateContractTypeAsync(ContractTypeCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<ContractTypeReadDto>> UpdateContractTypeAsync(int id, ContractTypeUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteContractTypeAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── WorkingCalendar ──────────────────────────────────────
    Task<ServiceResult<IEnumerable<WorkingCalendarReadDto>>> GetAllWorkingCalendarsAsync(CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<WorkingCalendarReadDto>>> GetWorkingCalendarAsync(int companyId, CancellationToken ct = default);
    Task<ServiceResult<WorkingCalendarReadDto>> GetWorkingCalendarDayByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<WorkingCalendarReadDto>> UpsertWorkingCalendarDayAsync(WorkingCalendarCreateDto dto, int userId, CancellationToken ct = default);
    Task<ServiceResult<WorkingCalendarReadDto>> UpdateWorkingCalendarDayAsync(int id, WorkingCalendarUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteWorkingCalendarDayAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── Holiday ──────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<HolidayReadDto>>> GetHolidaysAsync(int? companyId, int? year, CancellationToken ct = default);
    Task<ServiceResult<HolidayReadDto>> GetHolidayByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<HolidayReadDto>> CreateHolidayAsync(HolidayCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<HolidayReadDto>> UpdateHolidayAsync(int id, HolidayUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteHolidayAsync(int id, int deletedBy, CancellationToken ct = default);
    /// <summary>Vérifie si une date est fériée pour une société.</summary>
    Task<ServiceResult<bool>> CheckHolidayAsync(int? companyId, DateOnly date, CancellationToken ct = default);
    /// <summary>Liste des types de jours fériés (ex: National, Religieux).</summary>
    Task<ServiceResult<IEnumerable<object>>> GetHolidayTypesAsync(CancellationToken ct = default);

    // ── CompanyDocument ──────────────────────────────────────
    Task<ServiceResult<IEnumerable<CompanyDocumentReadDto>>> GetAllDocumentsAsync(CancellationToken ct = default);
    Task<ServiceResult<CompanyDocumentReadDto>> GetDocumentByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<CompanyDocumentReadDto>>> GetDocumentsAsync(int companyId, CancellationToken ct = default);
    Task<ServiceResult<CompanyDocumentReadDto>> CreateDocumentAsync(CompanyDocumentCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<CompanyDocumentReadDto>> UpdateDocumentAsync(int id, CompanyDocumentUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteDocumentAsync(int id, int deletedBy, CancellationToken ct = default);
}
/// <summary>
/// Initialise les données par défaut d'une nouvelle entreprise.
/// Miroir exact de ICompanyOnboardingService du source.
/// Idempotent : ne recrée pas ce qui existe déjà.
/// Implémenté en Phase 3 par CompanyOnboardingService.
/// </summary>
public interface ICompanyOnboardingService
{
    Task OnboardAsync(int companyId, int userId, CancellationToken ct = default);
}

/// <summary>
/// Gestion des fichiers physiques liés aux documents d'entreprise.
/// Implémenté en Phase 3 par CompanyDocumentService (IWebHostEnvironment).
/// </summary>
public interface ICompanyDocumentService
{
    Task<ServiceResult<string>> SaveFileAsync(IFormFile file, int companyId, string? documentType, CancellationToken ct = default);
    Task<ServiceResult> DeleteFileAsync(string filePath, CancellationToken ct = default);
    Task<ServiceResult<(byte[] fileBytes, string contentType, string fileName)>> GetFileAsync(string filePath, CancellationToken ct = default);
}
