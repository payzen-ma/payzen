namespace payzen_backend.Models.Dashboard.Dtos
{
    /// <summary>
    /// Full HR dashboard payload for the tabbed PayZen dashboard.
    /// Endpoint target: GET /api/dashboard/hr?month=yyyy-MM
    /// </summary>
    public class DashboardHrDto
    {
        public DashboardHrMetaDto Meta { get; set; } = new();
        public DashboardHrVueGlobaleDto VueGlobale { get; set; } = new();
        public DashboardHrMouvementsDto MouvementsRh { get; set; } = new();
        public DashboardHrMasseSalarialeDto MasseSalariale { get; set; } = new();
        public DashboardHrPariteDiversiteDto PariteDiversite { get; set; } = new();
        public DashboardHrConformiteSocialeDto ConformiteSociale { get; set; } = new();
    }

    /// <summary>
    /// Raw HR dataset used for client-side filtering and local aggregation.
    /// Endpoint target: GET /api/dashboard/hr/raw?month=yyyy-MM
    /// </summary>
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

    public class DashboardHrMetaDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        /// <summary>
        /// Format yyyy-MM
        /// </summary>
        public string Month { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; set; }
    }

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
        /// <summary>
        /// Format yyyy-MM
        /// </summary>
        public string Month { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class DashboardHrDepartmentCountDto
    {
        public string Department { get; set; } = string.Empty;
        public int Count { get; set; }
    }

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

    public enum DashboardHrMovementType
    {
        ENTRY = 1,
        EXIT = 2
    }

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
        /// <summary>
        /// Format yyyy-MM
        /// </summary>
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

    public enum DashboardHrDeclarationType
    {
        CNSS = 1,
        AMO = 2,
        IR = 3,
        OTHER = 4
    }

    public enum DashboardHrDeclarationStatus
    {
        PENDING = 1,
        SUBMITTED = 2,
        REJECTED = 3,
        OVERDUE = 4
    }
}
