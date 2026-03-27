using Payzen.Domain.Enums.Dashboard;

namespace Payzen.Application.DTOs.Dashboard;

// ════════════════════════════════════════════════════════════
// META
// ════════════════════════════════════════════════════════════

public class DashboardHrMetaDto
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    /// <summary>Format yyyy-MM</summary>
    public string Month { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// FULL HR DASHBOARD  — GET /api/dashboard/hr?month=yyyy-MM
// ════════════════════════════════════════════════════════════

public class DashboardHrDto
{
    public DashboardHrMetaDto Meta { get; set; } = new();
    public DashboardHrVueGlobaleDto VueGlobale { get; set; } = new();
    public DashboardHrMouvementsDto MouvementsRh { get; set; } = new();  // note: MouvementsRh (pas MouvementsRH)
    public DashboardHrMasseSalarialeDto MasseSalariale { get; set; } = new();
    public DashboardHrPariteDiversiteDto PariteDiversite { get; set; } = new();
    public DashboardHrConformiteSocialeDto ConformiteSociale { get; set; } = new();
}

// ════════════════════════════════════════════════════════════
// RAW — GET /api/dashboard/hr/raw?month=yyyy-MM
// ════════════════════════════════════════════════════════════

public class DashboardHrRawDto
{
    public DashboardHrMetaDto Meta { get; set; } = new();
    public List<DashboardHrRawEmployeeDto> Employees { get; set; } = new();
    public List<DashboardHrRawContractDto> Contracts { get; set; } = new();
    public List<DashboardHrRawSalaryDto> Salaries { get; set; } = new();
}

public class DashboardHrRawEmployeeDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string GenderCode { get; set; } = string.Empty;
}

public class DashboardHrRawContractDto
{
    public int EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Position { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
}

public class DashboardHrRawSalaryDto
{
    public int EmployeeId { get; set; }
    public decimal BaseSalary { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

// ════════════════════════════════════════════════════════════
// VUE GLOBALE
// ════════════════════════════════════════════════════════════

public class DashboardHrVueGlobaleDto
{
    public DashboardHrVueGlobaleKpisDto Kpis { get; set; } = new();
    public List<DashboardHrMonthHeadcountDto> EffectifEvolution6M { get; set; } = new();
    public List<DashboardHrDepartmentCountDto> RepartitionDepartement { get; set; } = new();
}

public class DashboardHrVueGlobaleKpisDto
{
    public int EffectifTotal { get; set; }
    public decimal MasseSalarialeMad { get; set; }
    public decimal Turnover12mPct { get; set; }
    public DashboardHrParityRatioDto Parite { get; set; } = new();
}

public class DashboardHrParityRatioDto
{
    public decimal FemalePct { get; set; }
    public decimal MalePct { get; set; }
}

public class DashboardHrMonthHeadcountDto
{
    /// <summary>Format yyyy-MM</summary>
    public string Month { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class DashboardHrDepartmentCountDto
{
    public string Department { get; set; } = string.Empty;
    public int Count { get; set; }
}

// ════════════════════════════════════════════════════════════
// MOUVEMENTS RH
// ════════════════════════════════════════════════════════════

public class DashboardHrMouvementsDto
{
    public DashboardHrMovementSummaryDto Summary { get; set; } = new();
    public List<DashboardHrMovementRowDto> Rows { get; set; } = new();
}

public class DashboardHrMovementSummaryDto
{
    public int Entrees { get; set; }
    public int Sorties { get; set; }
    public int SoldeNet { get; set; }
    public decimal RetentionPct { get; set; }
}

public class DashboardHrMovementRowDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DashboardHrMovementType MovementType { get; set; }
}

// ════════════════════════════════════════════════════════════
// MASSE SALARIALE
// ════════════════════════════════════════════════════════════

public class DashboardHrMasseSalarialeDto
{
    public DashboardHrMasseSalarialeKpisDto Kpis { get; set; } = new();
    public List<DashboardHrMonthAmountDto> Brut12m { get; set; } = new();
    public List<DashboardHrDepartmentPayrollDto> RepartitionDepartement { get; set; } = new();
}

public class DashboardHrMasseSalarialeKpisDto
{
    public decimal BrutTotalMad { get; set; }
    public decimal NetTotalMad { get; set; }
    public decimal ChargesPatronalesMad { get; set; }
    public decimal CoutTotalEmployeurMad { get; set; }
}

public class DashboardHrMonthAmountDto
{
    /// <summary>Format yyyy-MM</summary>
    public string Month { get; set; } = string.Empty;
    public decimal ValueMad { get; set; }
}

public class DashboardHrDepartmentPayrollDto
{
    public string Department { get; set; } = string.Empty;
    public int Employees { get; set; }
    public decimal AmountMad { get; set; }
    public decimal SharePct { get; set; }
}

// ════════════════════════════════════════════════════════════
// PARITÉ & DIVERSITÉ
// ════════════════════════════════════════════════════════════

public class DashboardHrPariteDiversiteDto
{
    public DashboardHrPariteDiversiteKpisDto Kpis { get; set; } = new();
    public List<DashboardHrPariteDepartmentDto> PariteDepartement { get; set; } = new();
    public List<DashboardHrPariteHierarchyDto> PariteNiveauHierarchique { get; set; } = new();
}

public class DashboardHrPariteDiversiteKpisDto
{
    public int EffectifFemmes { get; set; }
    public int EffectifHommes { get; set; }
    public decimal EcartSalarialPct { get; set; }
}

public class DashboardHrPariteDepartmentDto
{
    public string Department { get; set; } = string.Empty;
    public int FemaleCount { get; set; }
    public int MaleCount { get; set; }
    public decimal FemalePct { get; set; }
}

public class DashboardHrPariteHierarchyDto
{
    public string Level { get; set; } = string.Empty;
    public int Total { get; set; }
    public int FemaleCount { get; set; }
    public decimal FemalePct { get; set; }
}

// ════════════════════════════════════════════════════════════
// CONFORMITÉ SOCIALE
// ════════════════════════════════════════════════════════════

public class DashboardHrConformiteSocialeDto
{
    public DashboardHrConformiteKpisDto Kpis { get; set; } = new();
    public List<DashboardHrDeclarationDto> Declarations { get; set; } = new();
}

public class DashboardHrConformiteKpisDto
{
    public decimal CnssSalarialeMad { get; set; }
    public decimal CnssPatronaleMad { get; set; }
    public decimal AmoSalarialeMad { get; set; }
    public decimal IrRetenuSourceMad { get; set; }
}

public class DashboardHrDeclarationDto
{
    public DashboardHrDeclarationType Type { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal AmountMad { get; set; }
    public DateOnly Deadline { get; set; }
    public DashboardHrDeclarationStatus Status { get; set; }
    public string Reference { get; set; } = string.Empty;
}



// ════════════════════════════════════════════════════════════
// DASHBOARD GÉNÉRAL (liste employés)
// ════════════════════════════════════════════════════════════

public class DashboardResponseDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public List<EmployeeDashboardItemDto> Employees { get; set; } = new();
    public List<string> Departements { get; set; } = new();
    public List<string> statuses { get; set; } = new();
}

public class EmployeeDashboardItemDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string statuses { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public int MissingDocuments { get; set; }
    public string ContractType { get; set; } = string.Empty;  // 'CDI' | 'CDD' | 'Stage'
    public string? Manager { get; set; }
    /// <summary>Identifiant du compte utilisateur lié à l'employé (si présent).</summary>
    public int? UserId { get; set; }
}

// ════════════════════════════════════════════════════════════
// DASHBOARD BACKOFFICE (vue globale tous clients)
// ════════════════════════════════════════════════════════════

public class DashboardSummaryDto
{
    public int TotalCompanies { get; set; }
    public int TotalEmployees { get; set; }
    public int AccountingFirmsCount { get; set; }
    public double AvgEmployeesPerCompany { get; set; }
    public List<DistributionBucketDto> EmployeeDistribution { get; set; } = new();
    public List<RecentCompanyDto> RecentCompanies { get; set; } = new();
    public DateTimeOffset AsOf { get; set; }
}

public class DistributionBucketDto
{
    public string Bucket { get; set; } = string.Empty;
    public int CompaniesCount { get; set; }
    public int EmployeesCount { get; set; }
    public double Percentage { get; set; }
}

public class RecentCompanyDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CountryName { get; set; }
    public string? CityName { get; set; }
    public int EmployeesCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// DASHBOARD EXPERT
// ════════════════════════════════════════════════════════════

public class ExpertDashboardDto
{
    public int ExpertCompanyId { get; set; }
    public int TotalClients { get; set; }
    public int TotalEmployees { get; set; }
    public DateTimeOffset AsOf { get; set; }
}

// ════════════════════════════════════════════════════════════
// DASHBOARD EMPLOYEE
// ════════════════════════════════════════════════════════════

public class EmployeeDashboardDataDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string Matricule { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public string Seniority { get; set; } = string.Empty;

    public decimal SalaryNet { get; set; }
    public string PaidDate { get; set; } = string.Empty;

    public decimal LeavesRemaining { get; set; }
    public decimal LeavesTotal { get; set; }

    public int PresenceDays { get; set; }
    public int PresenceTotal { get; set; }

    public decimal ExtraHours { get; set; }

    public List<LeaveDetailDto> LeavesDetails { get; set; } = new();
    public List<ContractInfoDto> ContractInfo { get; set; } = new();
    public List<PayslipDetailDto> PayslipDetails { get; set; } = new();
    public List<EmployeeDocumentDto> Documents { get; set; } = new();
}

public class LeaveDetailDto
{
    public string Label { get; set; } = string.Empty;
    public decimal? Remaining { get; set; }
    public decimal? Total { get; set; }
    public string ColorClass { get; set; } = string.Empty;
    public bool? IsText { get; set; }
    public string? Text { get; set; }
}

public class ContractInfoDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool? IsTag { get; set; }
    public string? TagColor { get; set; }
}

public class PayslipDetailDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class EmployeeDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

// ════════════════════════════════════════════════════════════
// DASHBOARD CEO
// ════════════════════════════════════════════════════════════

public class CeoDashboardDto
{
    public List<CeoKpiDto> Kpis { get; set; } = new();
    public List<CeoChartPointDto> EvolutionChart { get; set; } = new();
    public List<CeoDepartmentDto> Departments { get; set; } = new();
    public List<CeoPayIndicatorDto> PayIndicators { get; set; } = new();
    public List<CeoAlertDto> Alerts { get; set; } = new();
}

public class CeoKpiDto
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string SubtitleColor { get; set; } = "text-gray-500";
}

public class CeoChartPointDto
{
    public string Month { get; set; } = string.Empty; // yyyy-MM
    public decimal NetMad { get; set; }
    public decimal ChargesMad { get; set; }
}

public class CeoDepartmentDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Color { get; set; } = "bg-gray-400";
    public decimal Percentage { get; set; }
}

public class CeoPayIndicatorDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? ValueColor { get; set; }
}

public class CeoAlertDto
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string DotColor { get; set; } = "bg-gray-400";
}