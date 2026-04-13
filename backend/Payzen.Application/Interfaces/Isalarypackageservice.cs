using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;

namespace Payzen.Application.Interfaces;

/// <summary>
/// CRUD des packages salariaux (templates officiels et d'entreprise), items et assignations.
/// Implémenté en Phase 3 par SalaryPackageService (AppDbContext).
/// </summary>
public interface ISalaryPackageService
{
    // ── Package ──────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<SalaryPackageReadDto>>> GetAllAsync(int? companyId, string? scope, string? status, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<SalaryPackageReadDto>>> GetTemplatesAsync(CancellationToken ct = default);
    Task<ServiceResult<SalaryPackageReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<SalaryPackageReadDto>> CreateAsync(SalaryPackageCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<SalaryPackageReadDto>> UpdateAsync(int id, SalaryPackageUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);

    /// <summary>Clone un template global vers une entreprise cliente (Copy-on-Write).</summary>
    Task<ServiceResult<SalaryPackageReadDto>> CloneAsync(int id, SalaryPackageCloneDto dto, int userId, CancellationToken ct = default);

    /// <summary>Nouvelle version d'un package (versioning).</summary>
    Task<ServiceResult<SalaryPackageReadDto>> NewVersionAsync(int id, int userId, CancellationToken ct = default);

    /// <summary>Publie un package (draft → published).</summary>
    Task<ServiceResult<SalaryPackageReadDto>> PublishAsync(int id, int userId, CancellationToken ct = default);

    /// <summary>Déprécie un package (published → deprecated).</summary>
    Task<ServiceResult<SalaryPackageReadDto>> DeprecateAsync(int id, int userId, CancellationToken ct = default);

    /// <summary>Duplique un package dans la même société.</summary>
    Task<ServiceResult<SalaryPackageReadDto>> DuplicateAsync(int id, SalaryPackageDuplicateDto dto, int userId, CancellationToken ct = default);

    // ── Items ─────────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<SalaryPackageItemReadDto>>> GetItemsAsync(int packageId, CancellationToken ct = default);
    Task<ServiceResult<SalaryPackageItemReadDto>> AddItemAsync(int packageId, SalaryPackageItemWriteDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<SalaryPackageItemReadDto>> UpdateItemAsync(int itemId, SalaryPackageItemWriteDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteItemAsync(int itemId, int deletedBy, CancellationToken ct = default);

    // ── Assignment ───────────────────────────────────────────
    Task<ServiceResult<IEnumerable<SalaryPackageAssignmentReadDto>>> GetAssignmentsAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<SalaryPackageAssignmentReadDto>>> GetAllAssignmentsAsync(int? companyId, int? employeeId, CancellationToken ct = default);
    Task<ServiceResult<SalaryPackageAssignmentReadDto>> GetAssignmentByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<SalaryPackageAssignmentReadDto>> AssignAsync(SalaryPackageAssignmentCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<SalaryPackageAssignmentReadDto>> UpdateAssignmentAsync(int id, SalaryPackageAssignmentUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> RevokeAssignmentAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── Salary Preview (SalaryPreviewController) ─────────────
    /// <summary>Calcule un résumé de paie en temps réel depuis le template editor.</summary>
    Task<ServiceResult<PayrollSummaryDto>> PreviewAsync(SalaryPreviewRequestDto dto, CancellationToken ct = default);
}
