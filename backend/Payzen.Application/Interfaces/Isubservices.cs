using Payzen.Application.Common;
using Payzen.Application.DTOs.Leave;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.DTOs.Referentiel;

namespace Payzen.Application.Interfaces;

// ════════════════════════════════════════════════════════════
// LEAVE SUB-SERVICES
// ════════════════════════════════════════════════════════════

public interface ILeaveTypeService
{
    Task<ServiceResult<IEnumerable<LeaveTypeReadDto>>> GetAllAsync(int? companyId, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypeReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypeReadDto>> CreateAsync(LeaveTypeCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypeReadDto>> PatchAsync(int id, LeaveTypePatchDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<LeaveTypePolicyReadDto>>> GetPoliciesAsync(int? companyId, int? leaveTypeId, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypePolicyReadDto>> CreatePolicyAsync(LeaveTypePolicyCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypePolicyReadDto>> PatchPolicyAsync(int id, LeaveTypePolicyPatchDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeletePolicyAsync(int id, int deletedBy, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<LeaveTypeLegalRuleReadDto>>> GetLegalRulesAsync(int? leaveTypeId, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypeLegalRuleReadDto>> CreateLegalRuleAsync(LeaveTypeLegalRuleCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteLegalRuleAsync(int id, int deletedBy, CancellationToken ct = default);
}

public interface ILeaveBalanceService
{
    Task<ServiceResult<IEnumerable<LeaveBalanceReadDto>>> GetByEmployeeAsync(int employeeId, int? leaveTypeId, CancellationToken ct = default);
    Task<ServiceResult<LeaveBalanceReadDto>> CreateAsync(LeaveBalanceCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<LeaveBalanceReadDto>> PatchAsync(int id, LeaveBalancePatchDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<LeaveCarryOverAgreementReadDto>>> GetCarryOverAgreementsAsync(int employeeId, CancellationToken ct = default);
    Task<ServiceResult<LeaveCarryOverAgreementReadDto>> CreateCarryOverAgreementAsync(LeaveCarryOverAgreementCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteCarryOverAgreementAsync(int id, int deletedBy, CancellationToken ct = default);
}

public interface ILeaveAuditLogService
{
    Task<ServiceResult<IEnumerable<LeaveAuditLogReadDto>>> GetAsync(int? companyId, int? leaveRequestId, CancellationToken ct = default);
    Task<ServiceResult<LeaveAuditLogReadDto>> CreateAsync(LeaveAuditLogCreateDto dto, CancellationToken ct = default);
}

// ════════════════════════════════════════════════════════════
// PAYROLL SUB-SERVICES
// ════════════════════════════════════════════════════════════

public interface IPayComponentService
{
    Task<ServiceResult<IEnumerable<PayComponentReadDto>>> GetAllAsync(bool? isActive, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<PayComponentReadDto>>> GetEffectiveAsync(DateTime? asOf, CancellationToken ct = default);
    Task<ServiceResult<PayComponentReadDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<PayComponentReadDto>> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<ServiceResult<PayComponentReadDto>> CreateAsync(PayComponentWriteDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<PayComponentReadDto>> UpdateAsync(int id, PayComponentWriteDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult<PayComponentReadDto>> NewVersionAsync(int id, int userId, CancellationToken ct = default);
    Task<ServiceResult<PayComponentReadDto>> DeactivateAsync(int id, int userId, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default);
}

public interface IReferentielPayrollService
{
    // Éléments
    Task<ServiceResult<IEnumerable<ReferentielElementListDto>>> GetElementsAsync(bool? isActive, CancellationToken ct = default);
    Task<ServiceResult<ReferentielElementDto>> GetElementByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<ReferentielElementDto>> CreateElementAsync(CreateReferentielElementDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<ReferentielElementDto>> UpdateElementAsync(int id, UpdateReferentielElementDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteElementAsync(int id, int deletedBy, CancellationToken ct = default);

    // Règles
    Task<ServiceResult<ElementRuleDto>> CreateRuleAsync(CreateElementRuleDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<ElementRuleDto>> UpdateRuleAsync(int id, UpdateElementRuleDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteRuleAsync(int id, int deletedBy, CancellationToken ct = default);

    // Paramètres légaux
    Task<ServiceResult<IEnumerable<LegalParameterDto>>> GetLegalParametersAsync(CancellationToken ct = default);
    Task<ServiceResult<LegalParameterDto>> CreateLegalParameterAsync(CreateLegalParameterDto dto, int createdBy, CancellationToken ct = default);

    // Ancienneté
    Task<ServiceResult<IEnumerable<AncienneteRateSetDto>>> GetRateSetsAsync(CancellationToken ct = default);
    Task<ServiceResult<AncienneteRateSetDto>> CreateRateSetAsync(CreateAncienneteRateSetDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<AncienneteRateSetDto>> CustomizeCompanyRatesAsync(CustomizeCompanyRatesDto dto, int userId, CancellationToken ct = default);
}

// ════════════════════════════════════════════════════════════
// REFERENTIEL (pays, villes, nationalités, programmes emploi, OvertimeRateRule)
// ════════════════════════════════════════════════════════════
public interface IReferentielService
{
    // Country
    Task<ServiceResult<IEnumerable<CountryReadDto>>> GetCountriesAsync(CancellationToken ct = default);
    Task<ServiceResult<CountryReadDto>> GetCountryByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<CountryReadDto>> CreateCountryAsync(CountryCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<CountryReadDto>> UpdateCountryAsync(int id, CountryUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteCountryAsync(int id, int deletedBy, CancellationToken ct = default);

    // City
    Task<ServiceResult<IEnumerable<CityReadDto>>> GetCitiesAsync(int? countryId, CancellationToken ct = default);
    Task<ServiceResult<CityReadDto>> GetCityByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<CityReadDto>> CreateCityAsync(CityCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<CityReadDto>> UpdateCityAsync(int id, CityUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteCityAsync(int id, int deletedBy, CancellationToken ct = default);

    // Nationality
    Task<ServiceResult<IEnumerable<NationalityReadDto>>> GetNationalitiesAsync(CancellationToken ct = default);
    Task<ServiceResult<NationalityReadDto>> CreateNationalityAsync(NationalityCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteNationalityAsync(int id, int deletedBy, CancellationToken ct = default);

    // MaritalStatus
    Task<ServiceResult<IEnumerable<MaritalStatusReadDto>>> GetMaritalStatusesAsync(CancellationToken ct = default);
    Task<ServiceResult<MaritalStatusReadDto>> GetMaritalStatusByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<MaritalStatusReadDto>> CreateMaritalStatusAsync(MaritalStatusCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<MaritalStatusReadDto>> UpdateMaritalStatusAsync(int id, MaritalStatusUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteMaritalStatusAsync(int id, int deletedBy, CancellationToken ct = default);

    // Gender
    Task<ServiceResult<IEnumerable<GenderReadDto>>> GetGendersAsync(CancellationToken ct = default);
    Task<ServiceResult<GenderReadDto>> GetGenderByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<GenderReadDto>> CreateGenderAsync(GenderCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<GenderReadDto>> UpdateGenderAsync(int id, GenderUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteGenderAsync(int id, int deletedBy, CancellationToken ct = default);

    // Status
    Task<ServiceResult<IEnumerable<StatusReadDto>>> GetStatusesAsync(CancellationToken ct = default);
    Task<ServiceResult<StatusReadDto>> GetStatusByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<StatusReadDto>> CreateStatusAsync(StatusCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<StatusReadDto>> UpdateStatusAsync(int id, StatusUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteStatusAsync(int id, int deletedBy, CancellationToken ct = default);

    // EducationLevel
    Task<ServiceResult<IEnumerable<EducationLevelReadDto>>> GetEducationLevelsAsync(CancellationToken ct = default);
    Task<ServiceResult<EducationLevelReadDto>> GetEducationLevelByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<EducationLevelReadDto>> CreateEducationLevelAsync(EducationLevelCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<EducationLevelReadDto>> UpdateEducationLevelAsync(int id, EducationLevelUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteEducationLevelAsync(int id, int deletedBy, CancellationToken ct = default);

    // LegalContractType (referentiel global)
    Task<ServiceResult<IEnumerable<LegalContractTypeReadDtos>>> GetLegalContractTypesAsync(CancellationToken ct = default);
    Task<ServiceResult<LegalContractTypeReadDtos>> GetLegalContractTypeByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<LegalContractTypeReadDtos>> CreateLegalContractTypeAsync(LegalContractTypeCreateDtos dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<LegalContractTypeReadDtos>> UpdateLegalContractTypeAsync(int id, LegalContractTypeUpdateDtos dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteLegalContractTypeAsync(int id, int deletedBy, CancellationToken ct = default);

    // StateEmploymentProgram
    Task<ServiceResult<IEnumerable<StateEmploymentProgramReadDto>>> GetStateEmploymentProgramsAsync(CancellationToken ct = default);
    Task<ServiceResult<StateEmploymentProgramReadDto>> GetStateEmploymentProgramByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<StateEmploymentProgramReadDto>> CreateStateEmploymentProgramAsync(StateEmploymentProgramCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<StateEmploymentProgramReadDto>> UpdateStateEmploymentProgramAsync(int id, StateEmploymentProgramUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteStateEmploymentProgramAsync(int id, int deletedBy, CancellationToken ct = default);

    // OvertimeRateRule
    Task<ServiceResult<IEnumerable<OvertimeRateRuleReadDto>>> GetOvertimeRateRulesAsync(bool? isActive, CancellationToken ct = default);
    Task<ServiceResult<OvertimeRateRuleReadDto>> GetOvertimeRateRuleByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<string>>> GetOvertimeRateRuleCategoriesAsync(CancellationToken ct = default);
    Task<ServiceResult<OvertimeRateRuleReadDto>> CreateOvertimeRateRuleAsync(OvertimeRateRuleCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<OvertimeRateRuleReadDto>> UpdateOvertimeRateRuleAsync(int id, OvertimeRateRuleUpdateDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult> DeleteOvertimeRateRuleAsync(int id, int deletedBy, CancellationToken ct = default);
}

// ════════════════════════════════════════════════════════════
// CONVERGENCE (CNSS vs DGI)
// ════════════════════════════════════════════════════════════

public interface IConvergenceService
{
    Task<ServiceResult<bool>> RecalculateAllAsync(CancellationToken ct = default);
    Task<ServiceResult<bool>> RecalculateElementAsync(int elementId, CancellationToken ct = default);
}