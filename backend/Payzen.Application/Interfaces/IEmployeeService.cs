using Payzen.Application.Common;
using Payzen.Application.DTOs.Dashboard;
using Payzen.Application.DTOs.Employee;
using Payzen.Domain.Enums;

namespace Payzen.Application.Interfaces;

/// <summary>
/// CRUD employé + toutes les sous-ressources.
/// </summary>
public interface IEmployeeService
{
    // ── Employee (core) ──────────────────────────────────────
    Task<ServiceResult<DashboardResponseDto>> GetAllAsync(int? companyId, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeReadDto>>> GetAllSimpleAsync(CancellationToken ct = default);
    Task<ServiceResult<EmployeeReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<EmployeeDetailDto>> GetDetailAsync(int id, int requestingUserId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeReadDto>> GetCurrentAsync(int userId, CancellationToken ct = default);
    Task<ServiceResult<object>> GetSummaryAsync(int? companyId, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<object>>> GetHistoryAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<DashboardResponseDto>> GetByCompanyAsync(int companyId, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeReadDto>>> GetByDepartementAsync(int departementId, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeReadDto>>> GetSubordinatesAsync(int managerId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeFormDataDto>> GetFormDataAsync(int? companyId, int requestingUserId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeReadDto>> CreateAsync(EmployeeCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeReadDto>> UpdateAsync(int id, EmployeeUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<SageImportResultDto>> ImportFromSageAsync(Stream csvStream, int? companyId, int userId, int? month, int? year, bool preview, CancellationToken ct = default);

    // ── Contract ─────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<EmployeeContractReadDto>>> GetContractsAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeContractReadDto>> CreateContractAsync(EmployeeContractCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeContractReadDto>> UpdateContractAsync(int id, EmployeeContractUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteContractAsync(int id, int deletedBy, CancellationToken ct = default);


    // ── Salary ───────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<EmployeeSalaryReadDto>>> GetSalariesAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeSalaryReadDto>> CreateSalaryAsync(EmployeeSalaryCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeSalaryReadDto>> UpdateSalaryAsync(int id, EmployeeSalaryUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteSalaryAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── SalaryComponent ──────────────────────────────────────
    Task<ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>> GetSalaryComponentsAsync(int salaryId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeSalaryComponentReadDto>> CreateSalaryComponentAsync(EmployeeSalaryComponentCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeSalaryComponentReadDto>> UpdateSalaryComponentAsync(int id, EmployeeSalaryComponentUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteSalaryComponentAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── Address ──────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<EmployeeAddressReadDto>>> GetAddressesAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeAddressReadDto>> CreateAddressAsync(EmployeeAddressCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeAddressReadDto>> UpdateAddressAsync(int id, EmployeeAddressUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteAddressAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── Document ─────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<EmployeeDocumentReadDto>>> GetDocumentsAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeDocumentReadDto>> CreateDocumentAsync(EmployeeDocumentCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeDocumentReadDto>> UpdateDocumentAsync(int id, EmployeeDocumentUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteDocumentAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── Child ────────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<EmployeeChildReadDto>>> GetChildrenAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeChildReadDto>> CreateChildAsync(EmployeeChildCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeChildReadDto>> UpdateChildAsync(int id, EmployeeChildUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteChildAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── Spouse ───────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<EmployeeSpouseReadDto>>> GetSpousesAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeSpouseReadDto>> CreateSpouseAsync(EmployeeSpouseCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeSpouseReadDto>> UpdateSpouseAsync(int id, EmployeeSpouseUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteSpouseAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeSpouseReadDto>> UpdateSpouseByEmployeeAsync(int employeeId, EmployeeSpouseUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteSpouseByEmployeeAsync(int employeeId, int deletedBy, CancellationToken ct = default);

    // ── Absence ──────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<EmployeeAbsenceReadDto>>> GetAbsencesAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeAbsenceStatsDto>> GetAbsenceStatsAsync(int companyId, int? employeeId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeAbsenceReadDto>> CreateAbsenceAsync(EmployeeAbsenceCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeAbsenceReadDto>> UpdateAbsenceAsync(int id, EmployeeAbsenceUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeAbsenceReadDto>> DecideAbsenceAsync(int id, EmployeeAbsenceDecisionDto dto, int decidedBy, CancellationToken ct = default);
    Task<ServiceResult> CancelAbsenceAsync(int id, EmployeeAbsenceCancellationDto dto, int cancelledBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteAbsenceAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── Overtime ─────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<EmployeeOvertimeListDto>>> GetOvertimesAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeOvertimeReadDto>> GetOvertimeByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<EmployeeOvertimeCreateOutcomeDto>> CreateOvertimeAsync(EmployeeOvertimeCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeOvertimeReadDto>> UpdateOvertimeAsync(int id, EmployeeOvertimeUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeOvertimeReadDto>> SubmitOvertimeAsync(int id, EmployeeOvertimeSubmitDto dto, int userId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeOvertimeReadDto>> DecideOvertimeAsync(int id, EmployeeOvertimeApprovalDto dto, int decidedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteOvertimeAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── Attendance ───────────────────────────────────────────
    Task<ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>> GetAttendancesAsync(int employeeId, DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<ServiceResult<EmployeeAttendanceReadDto>> CreateAttendanceAsync(EmployeeAttendanceCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<TimesheetImportResultDto>> ImportTimesheetAsync(int companyId, int month, int year, IEnumerable<object> rows, int userId, CancellationToken ct = default);

    // ── Category ─────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<EmployeeCategoryReadDto>>> GetCategoriesAsync(int companyId, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeCategoryReadDto>>> GetCategoriesByModeAsync(EmployeeCategoryMode mode, int? companyId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeCategoryReadDto>> GetCategoryByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<EmployeeCategoryReadDto>> CreateCategoryAsync(EmployeeCategoryCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeCategoryReadDto>> UpdateCategoryAsync(int id, EmployeeCategoryUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteCategoryAsync(int id, int deletedBy, CancellationToken ct = default);
}
