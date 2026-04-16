using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;
using Payzen.Domain.Enums;

namespace Payzen.Application.Interfaces;

/// <summary>Contrats employé. Implémenté en Phase 3.</summary>
public interface IEmployeeContractService
{
    Task<ServiceResult<IEnumerable<EmployeeContractReadDto>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeContractReadDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeContractReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<EmployeeContractReadDto>> CreateAsync(
        EmployeeContractCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeContractReadDto>> UpdateAsync(
        int id,
        EmployeeContractUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
}

/// <summary>Salaires et composantes salariales. Implémenté en Phase 3.</summary>
public interface IEmployeeSalaryService
{
    Task<ServiceResult<IEnumerable<EmployeeSalaryReadDto>>> GetAllSalariesAsync(CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeSalaryReadDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeSalaryReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeSalaryReadDto>>> GetByContractAsync(
        int contractId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeSalaryReadDto>> CreateAsync(
        EmployeeSalaryCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeSalaryReadDto>> UpdateAsync(
        int id,
        EmployeeSalaryUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>> GetComponentsAsync(
        int salaryId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeSalaryComponentReadDto>> GetComponentByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>> GetComponentsByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeSalaryComponentReadDto>> CreateComponentAsync(
        EmployeeSalaryComponentCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeSalaryComponentReadDto>> UpdateComponentAsync(
        int id,
        EmployeeSalaryComponentUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteComponentAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>> GetAllSalaryComponentsAsync(
        CancellationToken ct = default
    );
    Task<ServiceResult<object>> ReviseSalaryComponentAsync(
        int id,
        EmployeeSalaryComponentUpdateDto dto,
        int userId,
        CancellationToken ct = default
    );
}

/// <summary>Documents employé. Implémenté en Phase 3.</summary>
public interface IEmployeeDocumentService
{
    Task<ServiceResult<IEnumerable<EmployeeDocumentReadDto>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<EmployeeDocumentReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeDocumentReadDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeDocumentReadDto>> CreateAsync(
        EmployeeDocumentCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeDocumentReadDto>> UpdateAsync(
        int id,
        EmployeeDocumentUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
}

/// <summary>Adresses employé. Implémenté en Phase 3.</summary>
public interface IEmployeeAddressService
{
    Task<ServiceResult<IEnumerable<EmployeeAddressReadDto>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<EmployeeAddressReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeAddressReadDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAddressReadDto>> CreateAsync(
        EmployeeAddressCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAddressReadDto>> UpdateAsync(
        int id,
        EmployeeAddressUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
}

/// <summary>Conjoint et enfants. Implémenté en Phase 3.</summary>
public interface IEmployeeFamilyService
{
    Task<ServiceResult<EmployeeChildReadDto>> GetChildByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeChildReadDto>>> GetChildrenAsync(
        int employeeId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeChildReadDto>> CreateChildAsync(
        EmployeeChildCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeChildReadDto>> UpdateChildAsync(
        int id,
        EmployeeChildUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteChildAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeSpouseReadDto>>> GetSpousesAsync(
        int employeeId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeSpouseReadDto>> CreateSpouseAsync(
        EmployeeSpouseCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeSpouseReadDto>> UpdateSpouseAsync(
        int id,
        EmployeeSpouseUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteSpouseAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeSpouseReadDto>> UpdateSpouseByEmployeeAsync(
        int employeeId,
        EmployeeSpouseUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteSpouseByEmployeeAsync(int employeeId, int deletedBy, CancellationToken ct = default);
}

/// <summary>Pointage et pauses. Implémenté en Phase 3.</summary>
public interface IEmployeeAttendanceService
{
    Task<ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>> GetAllAsync(
        DateOnly? startDate,
        DateOnly? endDate,
        int? employeeId,
        AttendanceStatus? status,
        bool includeBreaks = false,
        CancellationToken ct = default
    );
    Task<ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>> GetByEmployeeAsync(
        int employeeId,
        DateOnly? from,
        DateOnly? to,
        bool includeBreaks = false,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAttendanceReadDto>> GetByIdAsync(
        int id,
        bool includeBreaks = false,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAttendanceReadDto>> CreateAsync(
        EmployeeAttendanceCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAttendanceReadDto>> UpdateAsync(
        int id,
        EmployeeAttendanceUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAttendanceReadDto>> PutAsync(
        int id,
        EmployeeAttendanceCreateDto dto,
        int userId,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeAttendanceReadDto>> CheckInAsync(
        int employeeId,
        EmployeeAttendanceCreateDto? dto,
        int userId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAttendanceReadDto>> CheckOutAsync(
        int employeeId,
        int? attendanceId,
        int userId,
        CancellationToken ct = default
    );
    Task<ServiceResult<TimesheetImportResultDto>> ImportTimesheetAsync(
        int companyId,
        int month,
        int year,
        IEnumerable<object> rows,
        int userId,
        CancellationToken ct = default
    );
}

/// <summary>Absences employé. Implémenté en Phase 3.</summary>
public interface IEmployeeAbsenceService
{
    Task<ServiceResult<IEnumerable<EmployeeAbsenceReadDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    );
    Task<ServiceResult<IEnumerable<EmployeeAbsenceReadDto>>> GetByCompanyAsync(
        int companyId,
        int? employeeId,
        DateOnly? startDate,
        DateOnly? endDate,
        AbsenceDurationType? durationType,
        AbsenceStatus? status,
        string? absenceType,
        int limit,
        CancellationToken ct = default
    );
    Task<ServiceResult<IEnumerable<string>>> GetDistinctTypesAsync(int companyId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeAbsenceReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<EmployeeAbsenceStatsDto>> GetStatsAsync(
        int companyId,
        int? employeeId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAbsenceReadDto>> CreateAsync(
        EmployeeAbsenceCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAbsenceReadDto>> UpdateAsync(
        int id,
        EmployeeAbsenceUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAbsenceReadDto>> DecideAsync(
        int id,
        EmployeeAbsenceDecisionDto dto,
        int decidedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAbsenceReadDto>> SubmitAsync(int id, int userId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeAbsenceReadDto>> ApproveAsync(
        int id,
        int userId,
        string? comment,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAbsenceReadDto>> RejectAsync(
        int id,
        int userId,
        string reason,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAbsenceReadDto>> CancelAsync(
        int id,
        EmployeeAbsenceCancellationDto dto,
        int cancelledBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
}

/// <summary>Heures supplémentaires employé. Implémenté en Phase 3.</summary>
public interface IEmployeeOvertimeService
{
    Task<ServiceResult<IEnumerable<EmployeeOvertimeListDto>>> GetAllAsync(
        int? companyId,
        int? employeeId,
        OvertimeStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool? isProcessedInPayroll,
        CancellationToken ct = default
    );
    Task<ServiceResult<IEnumerable<EmployeeOvertimeListDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeOvertimeReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<EmployeeOvertimeCreateOutcomeDto>> CreateAsync(
        EmployeeOvertimeCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeOvertimeReadDto>> UpdateAsync(
        int id,
        EmployeeOvertimeUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeOvertimeReadDto>> SubmitAsync(
        int id,
        EmployeeOvertimeSubmitDto dto,
        int userId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeOvertimeReadDto>> DecideAsync(
        int id,
        EmployeeOvertimeApprovalDto dto,
        int decidedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeOvertimeReadDto>> CancelAsync(int id, int userId, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<EmployeeOvertimeStatsDto>> GetStatsAsync(
        int companyId,
        int? employeeId,
        CancellationToken ct = default
    );
}

/// <summary>Pauses de pointage avec recalcul automatique des heures travaillées.</summary>
public interface IEmployeeAttendanceBreakService
{
    Task<ServiceResult<EmployeeAttendanceBreakReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<EmployeeAttendanceBreakReadDto>>> GetByAttendanceAsync(
        int attendanceId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAttendanceBreakReadDto>> StartBreakAsync(
        StartBreakDto dto,
        int userId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAttendanceBreakReadDto>> EndBreakAsync(
        int attendanceId,
        EndBreakDto dto,
        int userId,
        CancellationToken ct = default
    );
    Task<ServiceResult<EmployeeAttendanceBreakReadDto>> UpdateAsync(
        int id,
        EmployeeAttendanceBreakUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<object>> GetTotalBreakTimeAsync(int attendanceId, CancellationToken ct = default);
}
