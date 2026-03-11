using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Payroll.Dtos;

namespace payzen_backend.Services
{
    /// <summary>
    /// Moroccan Payroll Calculation Service (Morocco 2025 Regulations)
    /// Implements CNSS, CIMR, IR, seniority bonus, and professional expenses calculations
    /// </summary>
    public interface IMoroccanPayrollService
    {
        decimal CalculateSeniorityBonus(decimal baseSalary, int yearsOfService);
        Task<decimal> CalculateSeniorityBonusAsync(decimal baseSalary, int yearsOfService);

        /// <summary>
        /// Calculate seniority bonus for a specific company as of a specific date.
        /// - First checks for company-specific rates
        /// - Falls back to legal default if no company-specific rates exist
        /// - Uses asOfDate for historical calculations (e.g., recalculating old payslips)
        /// </summary>
        Task<decimal> CalculateSeniorityBonusForCompanyAsync(
            decimal baseSalary,
            int yearsOfService,
            int companyId,
            DateOnly? asOfDate = null);

        CnssResult CalculateCnss(decimal grossSalary);
        CimrResult CalculateCimr(decimal grossSalary, CimrConfigDto? config);
        decimal CalculateProfessionalExpenses(decimal grossTaxable);
        IrResult CalculateIncomeTax(decimal netTaxableIncome, int dependents = 0);

        /// <summary>CNSS using LegalParameter at date (fallback to constants).</summary>
        Task<CnssResult> CalculateCnssAsync(decimal grossSalary, DateOnly asOfDate);
        /// <summary>CIMR using CNSS ceiling from LegalParameter when applicable.</summary>
        Task<CimrResult> CalculateCimrAsync(decimal grossSalary, CimrConfigDto? config, DateOnly asOfDate);
        /// <summary>Professional expenses using LegalParameter at date.</summary>
        Task<decimal> CalculateProfessionalExpensesAsync(decimal grossTaxable, DateOnly asOfDate);
        /// <summary>Income tax using IR brackets from LegalParameter at date.</summary>
        Task<IrResult> CalculateIncomeTaxAsync(decimal netTaxableIncome, int dependents, DateOnly asOfDate);

        /// <summary>
        /// Calculate full payroll from request. Uses rule-driven bases when items have ReferentielElementId.
        /// </summary>
        Task<PayrollSummaryDto> CalculateFullPayrollAsync(SalaryPreviewRequestDto request);
    }

    public class CnssResult
    {
        public decimal EmployeeContribution { get; set; }
        public decimal AmoEmployee { get; set; }
        public decimal EmployerContribution { get; set; }
        public decimal AllocationsFamiliales { get; set; }
        public decimal TaxeProfessionnelle { get; set; }
        public decimal AmoEmployer { get; set; }
        public decimal TotalEmployee { get; set; }
        public decimal TotalEmployer { get; set; }
    }

    public class CimrResult
    {
        public decimal EmployeeContribution { get; set; }
        public decimal EmployerContribution { get; set; }
        public string Regime { get; set; } = "NONE";
    }

    public class IrResult
    {
        public decimal GrossIr { get; set; }
        public decimal FamilyDeductions { get; set; }
        public decimal NetIr { get; set; }
        public string Bracket { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }

    public class MoroccanPayrollService : IMoroccanPayrollService
    {
        private readonly AppDbContext _db;
        private readonly IElementRuleResolutionService _ruleResolution;

        public MoroccanPayrollService(AppDbContext db, IElementRuleResolutionService ruleResolution)
        {
            _db = db;
            _ruleResolution = ruleResolution;
        }

        // CNSS Constants (Morocco 2025)
        private const decimal CNSS_CEILING = 6000m; // Plafond CNSS
        private const decimal CNSS_EMPLOYEE_RATE_PS = 0.0448m; // Prestations sociales
        private const decimal CNSS_EMPLOYEE_RATE_AMO = 0.0226m; // AMO employee
        private const decimal CNSS_EMPLOYER_RATE_PS = 0.0898m; // Prestations sociales
        private const decimal CNSS_EMPLOYER_RATE_AF = 0.0640m; // Allocations familiales
        private const decimal CNSS_EMPLOYER_RATE_FP = 0.0160m; // Formation professionnelle
        private const decimal CNSS_EMPLOYER_RATE_AMO = 0.0411m; // AMO employer

        // Professional Expenses Constants
        private const decimal PROF_EXP_THRESHOLD = 6500m;
        private const decimal PROF_EXP_RATE_LOW = 0.35m;
        private const decimal PROF_EXP_CAP_LOW = 2916.67m;
        private const decimal PROF_EXP_RATE_HIGH = 0.25m;
        private const decimal PROF_EXP_CAP_HIGH = 2500m;

        // IR Tax Brackets (Morocco 2025 - Monthly)
        private static readonly (decimal max, decimal rate, decimal deduction, string label)[] IrBrackets = {
            (3333.33m, 0.00m, 0m, "0%"),
            (5000.00m, 0.10m, 333.33m, "10%"),
            (6666.67m, 0.20m, 833.33m, "20%"),
            (8333.33m, 0.30m, 1500.00m, "30%"),
            (15000.00m, 0.34m, 1833.33m, "34%"),
            (decimal.MaxValue, 0.37m, 2283.33m, "37%")
        };

        // Family Deduction (per dependent, max 6)
        private const decimal FAMILY_DEDUCTION_PER_PERSON = 30m;
        private const int MAX_DEPENDENTS = 6;

        /// <summary>
        /// Calculate seniority bonus (Prime d'ancienneté) based on years of service
        /// Fetches the current active legal default rate set from the database
        /// </summary>
        public async Task<decimal> CalculateSeniorityBonusAsync(decimal baseSalary, int yearsOfService)
        {
            if (yearsOfService < 2) return 0;

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var rateSet = await _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .Where(s =>
                    s.CompanyId == null && // Legal default only
                    s.IsLegalDefault &&
                    s.DeletedAt == null &&
                    s.EffectiveFrom <= today &&
                    (s.EffectiveTo == null || s.EffectiveTo >= today))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (rateSet == null)
                return 0; // No active rate set configured

            var rate = rateSet.GetRateForYears(yearsOfService);
            return Math.Round(baseSalary * rate, 2);
        }

        /// <summary>
        /// Calculate seniority bonus for a specific company as of a specific date.
        /// - First checks for company-specific rates
        /// - Falls back to legal default if no company-specific rates exist
        /// - Uses asOfDate for historical calculations (e.g., recalculating old payslips)
        /// </summary>
        public async Task<decimal> CalculateSeniorityBonusForCompanyAsync(
            decimal baseSalary,
            int yearsOfService,
            int companyId,
            DateOnly? asOfDate = null)
        {
            if (yearsOfService < 2) return 0;

            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            // First, try to find company-specific rates
            var rateSet = await _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .Where(s =>
                    s.CompanyId == companyId &&
                    s.DeletedAt == null &&
                    s.EffectiveFrom <= checkDate &&
                    (s.EffectiveTo == null || s.EffectiveTo >= checkDate))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();

            // Fall back to legal default if no company-specific rates
            if (rateSet == null)
            {
                rateSet = await _db.AncienneteRateSets
                    .AsNoTracking()
                    .Include(s => s.Rates)
                    .Where(s =>
                        s.CompanyId == null &&
                        s.IsLegalDefault &&
                        s.DeletedAt == null &&
                        s.EffectiveFrom <= checkDate &&
                        (s.EffectiveTo == null || s.EffectiveTo >= checkDate))
                    .OrderByDescending(s => s.EffectiveFrom)
                    .FirstOrDefaultAsync();
            }

            if (rateSet == null)
                return 0; // No rate set configured

            var rate = rateSet.GetRateForYears(yearsOfService);
            return Math.Round(baseSalary * rate, 2);
        }

        /// <summary>
        /// Synchronous version for backward compatibility (calls async internally)
        /// </summary>
        public decimal CalculateSeniorityBonus(decimal baseSalary, int yearsOfService)
        {
            return CalculateSeniorityBonusAsync(baseSalary, yearsOfService).GetAwaiter().GetResult();
        }

        private async Task<decimal> GetParamAsync(string label, DateOnly asOfDate, decimal fallback)
        {
            var v = await _ruleResolution.GetParameterValueEffectiveAtAsync(label, asOfDate);
            return v ?? fallback;
        }

        /// <summary>
        /// Calculate CNSS contributions (employee and employer). Sync version uses constants.
        /// </summary>
        public CnssResult CalculateCnss(decimal grossSalary)
        {
            return CalculateCnssAsync(grossSalary, DateOnly.FromDateTime(DateTime.UtcNow)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Calculate CNSS using LegalParameter at asOfDate (fallback to constants if missing).
        /// </summary>
        public async Task<CnssResult> CalculateCnssAsync(decimal grossSalary, DateOnly asOfDate)
        {
            var ceiling = await GetParamAsync("CNSS_PLAFOND", asOfDate, CNSS_CEILING);
            var psEmp = await GetParamAsync("CNSS_PS_EMPLOYEE_RATE", asOfDate, CNSS_EMPLOYEE_RATE_PS);
            var amoEmp = await GetParamAsync("CNSS_AMO_EMPLOYEE_RATE", asOfDate, CNSS_EMPLOYEE_RATE_AMO);
            var psPat = await GetParamAsync("CNSS_PS_EMPLOYER_RATE", asOfDate, CNSS_EMPLOYER_RATE_PS);
            var af = await GetParamAsync("CNSS_AF_EMPLOYER_RATE", asOfDate, CNSS_EMPLOYER_RATE_AF);
            var fp = await GetParamAsync("CNSS_FP_EMPLOYER_RATE", asOfDate, CNSS_EMPLOYER_RATE_FP);
            var amoPat = await GetParamAsync("CNSS_AMO_EMPLOYER_RATE", asOfDate, CNSS_EMPLOYER_RATE_AMO);

            var cappedSalary = Math.Min(grossSalary, ceiling);
            var employeePs = Math.Round(cappedSalary * psEmp, 2);
            var amoEmployee = Math.Round(grossSalary * amoEmp, 2);
            var employerPs = Math.Round(cappedSalary * psPat, 2);
            var afVal = Math.Round(grossSalary * af, 2);
            var fpVal = Math.Round(grossSalary * fp, 2);
            var amoEmployer = Math.Round(grossSalary * amoPat, 2);

            return new CnssResult
            {
                EmployeeContribution = employeePs,
                AmoEmployee = amoEmployee,
                EmployerContribution = employerPs,
                AllocationsFamiliales = afVal,
                TaxeProfessionnelle = fpVal,
                AmoEmployer = amoEmployer,
                TotalEmployee = employeePs + amoEmployee,
                TotalEmployer = employerPs + afVal + fpVal + amoEmployer
            };
        }

        /// <summary>
        /// Calculate CIMR contributions based on configuration. Sync version uses constants for ceiling.
        /// </summary>
        public CimrResult CalculateCimr(decimal grossSalary, CimrConfigDto? config)
        {
            return CalculateCimrAsync(grossSalary, config, DateOnly.FromDateTime(DateTime.UtcNow)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Calculate CIMR using CNSS ceiling from LegalParameter when regime is AL_MOUNASSIB.
        /// </summary>
        public async Task<CimrResult> CalculateCimrAsync(decimal grossSalary, CimrConfigDto? config, DateOnly asOfDate)
        {
            if (config == null || config.Regime == "NONE")
                return new CimrResult { Regime = "NONE" };

            decimal salaryBase = grossSalary;
            if (config.Regime == "AL_MOUNASSIB")
            {
                var cnssCeiling = await GetParamAsync("CNSS_PLAFOND", asOfDate, CNSS_CEILING);
                salaryBase = Math.Min(grossSalary, cnssCeiling);
            }

            var employeeContribution = Math.Round(salaryBase * config.EmployeeRate, 2);
            var employerRate = config.CustomEmployerRate ?? config.EmployerRate;
            var employerContribution = Math.Round(salaryBase * employerRate, 2);

            return new CimrResult
            {
                EmployeeContribution = employeeContribution,
                EmployerContribution = employerContribution,
                Regime = config.Regime
            };
        }

        /// <summary>
        /// Calculate professional expenses deduction. Sync version uses constants.
        /// </summary>
        public decimal CalculateProfessionalExpenses(decimal grossTaxable)
        {
            return CalculateProfessionalExpensesAsync(grossTaxable, DateOnly.FromDateTime(DateTime.UtcNow)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Calculate professional expenses using LegalParameter at asOfDate (threshold, rates, caps).
        /// </summary>
        public async Task<decimal> CalculateProfessionalExpensesAsync(decimal grossTaxable, DateOnly asOfDate)
        {
            if (grossTaxable <= 0) return 0;

            var threshold = await GetParamAsync("PROF_EXP_THRESHOLD_MONTHLY", asOfDate, PROF_EXP_THRESHOLD);
            var rateLow = await GetParamAsync("PROF_EXP_RATE_LOW", asOfDate, PROF_EXP_RATE_LOW);
            var rateHigh = await GetParamAsync("PROF_EXP_RATE_HIGH", asOfDate, PROF_EXP_RATE_HIGH);
            var capLow = await GetParamAsync("PROF_EXP_CAP_LOW_MONTHLY", asOfDate, PROF_EXP_CAP_LOW);
            var capHigh = await GetParamAsync("PROF_EXP_CAP_HIGH_MONTHLY", asOfDate, PROF_EXP_CAP_HIGH);

            // CGI Article 59: "ne dépassant pas" = <=
            decimal rate = grossTaxable <= threshold ? rateLow : rateHigh;
            decimal cap = grossTaxable <= threshold ? capLow : capHigh;
            var deduction = grossTaxable * rate;
            return Math.Round(Math.Min(deduction, cap), 2);
        }

        /// <summary>
        /// Calculate income tax (IR) using progressive brackets. Sync version uses constants.
        /// </summary>
        public IrResult CalculateIncomeTax(decimal netTaxableIncome, int dependents = 0)
        {
            return CalculateIncomeTaxAsync(netTaxableIncome, dependents, DateOnly.FromDateTime(DateTime.UtcNow)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Build IR brackets from LegalParameter at asOfDate (IR_2025_* from 2025-01-01, else IR_PRE2025_*).
        /// </summary>
        private async Task<(decimal max, decimal rate, decimal deduction, string label)[]> GetIrBracketsAsync(DateOnly asOfDate)
        {
            var prefix = asOfDate >= new DateOnly(2025, 1, 1) ? "IR_2025" : "IR_PRE2025";
            var brackets = new List<(decimal max, decimal rate, decimal deduction, string label)>();
            for (int i = 0; i <= 5; i++)
            {
                var max = await GetParamAsync($"{prefix}_B{i}_MAX", asOfDate, i < 6 ? IrBrackets[i].max : decimal.MaxValue);
                var rate = await GetParamAsync($"{prefix}_B{i}_RATE", asOfDate, IrBrackets[i].rate);
                var ded = await GetParamAsync($"{prefix}_B{i}_DEDUCTION", asOfDate, IrBrackets[i].deduction);
                var pct = (int)(rate * 100);
                brackets.Add((max, rate, ded, $"{pct}%"));
            }
            return brackets.ToArray();
        }

        /// <summary>
        /// Calculate income tax using IR brackets from LegalParameter at asOfDate.
        /// </summary>
        public async Task<IrResult> CalculateIncomeTaxAsync(decimal netTaxableIncome, int dependents, DateOnly asOfDate)
        {
            if (netTaxableIncome <= 0)
                return new IrResult { Bracket = "0%", Rate = 0 };

            var brackets = await GetIrBracketsAsync(asOfDate);
            decimal grossIr = 0;
            string bracketLabel = "0%";
            decimal bracketRate = 0;

            foreach (var (max, rate, deduction, label) in brackets)
            {
                if (netTaxableIncome <= max)
                {
                    grossIr = Math.Round(netTaxableIncome * rate - deduction, 2);
                    bracketLabel = label;
                    bracketRate = rate;
                    break;
                }
            }

            var effectiveDependents = Math.Min(dependents, MAX_DEPENDENTS);
            var familyDeduction = effectiveDependents * FAMILY_DEDUCTION_PER_PERSON;
            var netIr = Math.Max(0, grossIr - familyDeduction);

            return new IrResult
            {
                GrossIr = Math.Max(0, grossIr),
                FamilyDeductions = familyDeduction,
                NetIr = Math.Round(netIr, 2),
                Bracket = bracketLabel,
                Rate = bracketRate
            };
        }

        /// <summary>
        /// Calculate complete payroll from a salary preview request.
        /// When items have ReferentielElementId, CNSS/IR/CIMR bases use element rules; otherwise legacy IsTaxable/IsSocial/IsCIMR (and ExemptionLimit) apply.
        /// </summary>
        public async Task<PayrollSummaryDto> CalculateFullPayrollAsync(SalaryPreviewRequestDto request)
        {
            var baseSalary = request.BaseSalary;
            var yearsOfService = request.YearsOfService ?? 0;
            var dependents = request.Dependents;
            var payrollDate = request.PayrollDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            // Calculate seniority bonus if enabled
            decimal seniorityBonus = 0;
            if (request.AutoRules?.SeniorityBonusEnabled == true && yearsOfService >= 2)
            {
                seniorityBonus = CalculateSeniorityBonus(baseSalary, yearsOfService);
            }

            // Categorize items (for gross breakdown only)
            decimal allowances = 0;
            decimal bonuses = 0;
            decimal benefitsInKind = 0;

            // Resolve authority IDs for rule-driven items (CNSS, DGI/IR, CIMR)
            var cnssAuthorityId = await _ruleResolution.GetAuthorityIdByCodeAsync("CNSS");
            var irAuthorityId = await _ruleResolution.GetAuthorityIdByCodeAsync("DGI");
            if (irAuthorityId == null) irAuthorityId = await _ruleResolution.GetAuthorityIdByCodeAsync("IR");
            var cimrAuthorityId = await _ruleResolution.GetAuthorityIdByCodeAsync("CIMR");

            IReadOnlyDictionary<string, decimal> paramValues = new Dictionary<string, decimal>();
            if (cnssAuthorityId != null || irAuthorityId != null || cimrAuthorityId != null)
                paramValues = await _ruleResolution.GetParameterValuesEffectiveAtAsync(payrollDate);

            // Gross salary (base + seniority + all items) for PERCENTAGE/DUAL_CAP context
            decimal sumItems = request.Items.Sum(i => i.DefaultValue);
            var grossSalary = baseSalary + seniorityBonus + sumItems;

            decimal cnssBase = baseSalary + seniorityBonus;
            decimal grossTaxable = baseSalary + seniorityBonus;
            decimal cimrBase = baseSalary + seniorityBonus;

            foreach (var item in request.Items)
            {
                var value = item.DefaultValue;

                switch (item.Type?.ToLowerInvariant())
                {
                    case "allowance":
                        allowances += value;
                        break;
                    case "bonus":
                        bonuses += value;
                        break;
                    case "benefit_in_kind":
                        benefitsInKind += value;
                        break;
                }

                if (item.ReferentielElementId is int elementId)
                {
                    // Rule-driven: get rules per authority, compute exempt, add taxable to bases
                    decimal toCnss = value;
                    decimal toIr = value;
                    decimal toCimr = value;

                    if (cnssAuthorityId != null)
                    {
                        var cnssRules = await _ruleResolution.GetRulesForElementAuthorityEffectiveAtAsync(elementId, cnssAuthorityId.Value, payrollDate);
                        var exemptCnss = cnssRules.Count > 0
                            ? _ruleResolution.ComputeExemptAmount(cnssRules[0], value, baseSalary, grossSalary, grossSalary, paramValues)
                            : 0;
                        toCnss = Math.Max(0, value - exemptCnss);
                    }
                    if (irAuthorityId != null)
                    {
                        var irRules = await _ruleResolution.GetRulesForElementAuthorityEffectiveAtAsync(elementId, irAuthorityId.Value, payrollDate);
                        var exemptIr = irRules.Count > 0
                            ? _ruleResolution.ComputeExemptAmount(irRules[0], value, baseSalary, grossSalary, grossSalary, paramValues)
                            : 0;
                        toIr = Math.Max(0, value - exemptIr);
                    }
                    if (cimrAuthorityId != null)
                    {
                        var cimrRules = await _ruleResolution.GetRulesForElementAuthorityEffectiveAtAsync(elementId, cimrAuthorityId.Value, payrollDate);
                        var exemptCimr = cimrRules.Count > 0
                            ? _ruleResolution.ComputeExemptAmount(cimrRules[0], value, baseSalary, grossSalary, grossSalary, paramValues)
                            : 0;
                        toCimr = Math.Max(0, value - exemptCimr);
                    }

                    cnssBase += toCnss;
                    grossTaxable += toIr;
                    cimrBase += toCimr;
                }
                else
                {
                    // Legacy: IsTaxable / IsSocial / IsCIMR; cap by ExemptionLimit when present
                    var capped = item.ExemptionLimit.HasValue ? Math.Min(value, item.ExemptionLimit.Value) : value;
                    if (item.IsTaxable) grossTaxable += capped;
                    if (item.IsSocial) cnssBase += capped;
                    if (item.IsCIMR) cimrBase += capped;
                }
            }

            // CNSS / CIMR / professional expenses / IR using LegalParameter at payrollDate
            var cnss = await CalculateCnssAsync(cnssBase, payrollDate);
            var cimr = await CalculateCimrAsync(cimrBase, request.CimrConfig, payrollDate);
            var totalEmployeeDeductions = cnss.TotalEmployee + cimr.EmployeeContribution;
            var profExpenses = await CalculateProfessionalExpensesAsync(grossTaxable, payrollDate);
            var netTaxableIncome = grossTaxable - cnss.TotalEmployee - cimr.EmployeeContribution - profExpenses;
            netTaxableIncome = Math.Max(0, netTaxableIncome);
            var ir = await CalculateIncomeTaxAsync(netTaxableIncome, dependents, payrollDate);

            // Net salary
            var netSalary = grossSalary - cnss.TotalEmployee - cimr.EmployeeContribution - ir.NetIr;

            // Total employer cost
            var totalEmployerCost = cnss.TotalEmployer + cimr.EmployerContribution;

            // Total cost to company
            var totalCostToCompany = grossSalary + totalEmployerCost;

            // Resolved params for breakdown (avoid duplicate lookups)
            var plafondCnss = await GetParamAsync("CNSS_PLAFOND", payrollDate, CNSS_CEILING);
            var profThreshold = await GetParamAsync("PROF_EXP_THRESHOLD_MONTHLY", payrollDate, PROF_EXP_THRESHOLD);
            // CGI Article 59: "ne dépassant pas" = <=
            var tauxFraisPro = grossTaxable <= profThreshold
                ? await GetParamAsync("PROF_EXP_RATE_LOW", payrollDate, PROF_EXP_RATE_LOW)
                : await GetParamAsync("PROF_EXP_RATE_HIGH", payrollDate, PROF_EXP_RATE_HIGH);
            var plafondFraisPro = grossTaxable <= profThreshold
                ? await GetParamAsync("PROF_EXP_CAP_LOW_MONTHLY", payrollDate, PROF_EXP_CAP_LOW)
                : await GetParamAsync("PROF_EXP_CAP_HIGH_MONTHLY", payrollDate, PROF_EXP_CAP_HIGH);

            return new PayrollSummaryDto
            {
                BaseSalary = baseSalary,
                SeniorityBonus = seniorityBonus,
                Allowances = allowances,
                Bonuses = bonuses,
                BenefitsInKind = benefitsInKind,
                GrossSalary = grossSalary,

                CnssEmployee = cnss.EmployeeContribution,
                AmoEmployee = cnss.AmoEmployee,
                CimrEmployee = cimr.EmployeeContribution,
                TotalEmployeeDeductions = totalEmployeeDeductions,

                GrossTaxable = grossTaxable,
                ProfessionalExpenses = profExpenses,
                NetTaxableIncome = netTaxableIncome,

                IncomeTaxGross = ir.GrossIr,
                FamilyDeductions = ir.FamilyDeductions,
                IncomeTaxNet = ir.NetIr,

                NetSalary = Math.Round(netSalary, 2),

                CnssEmployer = cnss.EmployerContribution,
                AllocationsFamiliales = cnss.AllocationsFamiliales,
                TaxeProfessionnelle = cnss.TaxeProfessionnelle,
                AmoEmployer = cnss.AmoEmployer,
                CimrEmployer = cimr.EmployerContribution,
                TotalEmployerCost = totalEmployerCost,
                TotalCostToCompany = totalCostToCompany,

                CnssBreakdown = new CnssBreakdownDto
                {
                    PlafondCnss = plafondCnss,
                    SalaireBrutPlafonne = Math.Min(cnssBase, plafondCnss)
                },
                CimrBreakdown = new CimrBreakdownDto
                {
                    Regime = cimr.Regime,
                    SalaireReference = cimrBase,
                    TauxSalarial = request.CimrConfig?.EmployeeRate ?? 0,
                    TauxPatronal = request.CimrConfig?.CustomEmployerRate ?? request.CimrConfig?.EmployerRate ?? 0,
                    CotisationSalariale = cimr.EmployeeContribution,
                    CotisationPatronale = cimr.EmployerContribution
                },
                IrBreakdown = new IrBreakdownDto
                {
                    SalaireBrutImposable = grossTaxable,
                    FraisProfessionnels = profExpenses,
                    TauxFraisPro = tauxFraisPro,
                    PlafondFraisPro = plafondFraisPro,
                    SalaireNetImposable = netTaxableIncome,
                    TrancheTaux = ir.Bracket,
                    IrBrut = ir.GrossIr,
                    DeductionChargesFamille = ir.FamilyDeductions,
                    NombrePersonnesACharge = dependents,
                    IrNet = ir.NetIr
                }
            };
        }
    }
}
