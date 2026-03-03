namespace payzen_backend.Models.Payroll.Dtos
{
    /// <summary>
    /// Complete payroll calculation summary for Morocco 2025
    /// </summary>
    public class PayrollSummaryDto
    {
        // --- Gross Salary Components ---
        public decimal BaseSalary { get; set; }
        public decimal SeniorityBonus { get; set; }
        public decimal Allowances { get; set; }
        public decimal Bonuses { get; set; }
        public decimal BenefitsInKind { get; set; }
        public decimal GrossSalary { get; set; }

        // --- Employee Deductions ---
        public decimal CnssEmployee { get; set; }
        public decimal AmoEmployee { get; set; }
        public decimal CimrEmployee { get; set; }
        public decimal TotalEmployeeDeductions { get; set; }

        // --- Taxable Income Calculation ---
        public decimal GrossTaxable { get; set; }
        public decimal ProfessionalExpenses { get; set; }
        public decimal NetTaxableIncome { get; set; }

        // --- Income Tax ---
        public decimal IncomeTaxGross { get; set; }
        public decimal FamilyDeductions { get; set; }
        public decimal IncomeTaxNet { get; set; }

        // --- Net Salary ---
        public decimal NetSalary { get; set; }

        // --- Employer Costs ---
        public decimal CnssEmployer { get; set; }
        public decimal AllocationsFamiliales { get; set; }
        public decimal TaxeProfessionnelle { get; set; }
        public decimal AmoEmployer { get; set; }
        public decimal CimrEmployer { get; set; }
        public decimal TotalEmployerCost { get; set; }

        // --- Total Cost to Company ---
        public decimal TotalCostToCompany { get; set; }

        // --- Breakdown Details ---
        public CnssBreakdownDto? CnssBreakdown { get; set; }
        public CimrBreakdownDto? CimrBreakdown { get; set; }
        public IrBreakdownDto? IrBreakdown { get; set; }
    }

    /// <summary>
    /// CNSS contribution breakdown
    /// </summary>
    public class CnssBreakdownDto
    {
        public decimal PlafondCnss { get; set; } = 6000m;
        public decimal SalaireBrutPlafonne { get; set; }
        public decimal TauxSalarialPS { get; set; } = 0.0448m;
        public decimal TauxSalarialAMO { get; set; } = 0.0226m;
        public decimal TauxPatronalPS { get; set; } = 0.0898m;
        public decimal TauxPatronalAF { get; set; } = 0.0640m;
        public decimal TauxPatronalFP { get; set; } = 0.0160m;
        public decimal TauxPatronalAMO { get; set; } = 0.0411m;
    }

    /// <summary>
    /// CIMR contribution breakdown
    /// </summary>
    public class CimrBreakdownDto
    {
        public string Regime { get; set; } = "NONE";
        public decimal SalaireReference { get; set; }
        public decimal TauxSalarial { get; set; }
        public decimal TauxPatronal { get; set; }
        public decimal CotisationSalariale { get; set; }
        public decimal CotisationPatronale { get; set; }
    }

    /// <summary>
    /// Income tax (IR) calculation breakdown
    /// </summary>
    public class IrBreakdownDto
    {
        public decimal SalaireBrutImposable { get; set; }
        public decimal FraisProfessionnels { get; set; }
        public decimal TauxFraisPro { get; set; }
        public decimal PlafondFraisPro { get; set; }
        public decimal SalaireNetImposable { get; set; }
        public string TrancheTaux { get; set; } = string.Empty;
        public decimal IrBrut { get; set; }
        public decimal DeductionChargesFamille { get; set; }
        public int NombrePersonnesACharge { get; set; }
        public decimal IrNet { get; set; }
    }

    /// <summary>
    /// Request DTO for salary preview calculation
    /// </summary>
    public class SalaryPreviewRequestDto
    {
        public decimal BaseSalary { get; set; }
        public List<SalaryPackageItemWriteDto> Items { get; set; } = new();
        public CimrConfigDto? CimrConfig { get; set; }
        public AutoRulesDto? AutoRules { get; set; }
        public int? YearsOfService { get; set; }
        public int Dependents { get; set; } = 0;

        /// <summary>
        /// Payroll date for rule/parameter resolution (element rules and legal parameters effective at this date).
        /// Defaults to today when null.
        /// </summary>
        public DateOnly? PayrollDate { get; set; }
    }
}
