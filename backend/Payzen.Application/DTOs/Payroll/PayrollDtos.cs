using System.ComponentModel.DataAnnotations;
using Payzen.Domain.Enums;

namespace Payzen.Application.DTOs.Payroll;

// ════════════════════════════════════════════════════════════
// INPUT MOTEUR — miroir exact de EmployeePayrollDto (source)
// ════════════════════════════════════════════════════════════

public class EmployeePayrollDto
{
    // Infos personnelles
    public string FullName { get; set; } = string.Empty;
    public string? CinNumber { get; set; }
    public string? CnssNumber { get; set; }
    public string? CimrNumber { get; set; }
    public decimal? CimrEmployeeRate { get; set; }
    public decimal? CimrCompanyRate { get; set; }
    public bool HasPrivateInsurance { get; set; }
    public decimal? PrivateInsuranceRate { get; set; }
    public bool DisableAmo { get; set; }
    public string? MaritalStatus { get; set; }
    public int NumberOfChildren { get; set; }
    public bool HasSpouse { get; set; }

    // Contrat actif
    public string? ContractType { get; set; }
    public string? LegalContractType { get; set; }
    public string? StateEmploymentProgram { get; set; }
    public string? JobPosition { get; set; }
    public DateTime ContractStartDate { get; set; }
    public int AncienneteYears { get; set; }

    // Salaire
    public decimal BaseSalary { get; set; }
    public decimal? BaseSalaryHourly { get; set; }
    public List<PayrollSalaryComponentDto> SalaryComponents { get; set; } = new();

    // Package salarial
    public string? SalaryPackageName { get; set; }
    public List<PayrollPackageItemDto> PackageItems { get; set; } = new();

    // Absences du mois
    public List<PayrollAbsenceDto> Absences { get; set; } = new();

    // Heures supplémentaires du mois
    public List<PayrollOvertimeDto> Overtimes { get; set; } = new();

    // Congés pris ce mois
    public List<PayrollLeaveDto> Leaves { get; set; } = new();

    // Période de paie
    public int PayMonth { get; set; }
    public int PayYear { get; set; }

    // null = mensuel ; 1 = 1-15 ; 2 = 16-31
    public int? PayHalf { get; set; }

    // Heures travaillées importées (pointage)
    public decimal TotalWorkedHours { get; set; }
}

public class PayrollSalaryComponentDto
{
    public string ComponentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsSocial { get; set; }
    public bool IsCIMR { get; set; }
}

public class PayrollPackageItemDto
{
    public string Label { get; set; } = string.Empty;
    public string? Type { get; set; }
    public decimal DefaultValue { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsSocial { get; set; }
    public bool IsCIMR { get; set; }
    public decimal? ExemptionLimit { get; set; }
}

public class PayrollAbsenceDto
{
    public string? AbsenceType { get; set; }
    public DateTime AbsenceDate { get; set; }
    public string DurationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class PayrollOvertimeDto
{
    public DateTime OvertimeDate { get; set; }
    public decimal DurationInHours { get; set; }
    public decimal RateMultiplier { get; set; }
}

public class PayrollLeaveDto
{
    public string? LeaveType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal DaysCount { get; set; }
}

// ════════════════════════════════════════════════════════════
// PAYROLL RESULT
// ════════════════════════════════════════════════════════════

public class PayrollResultReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    public int? PayHalf { get; set; }

    public PayrollResultStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    // Salaire de base et heures
    public decimal? SalaireBase { get; set; }
    public decimal? PrimeAnciennete { get; set; }
    public decimal? PrimeAnciennteRate { get; set; }
    public decimal? HeuresSupp25 { get; set; }
    public decimal? HeuresSupp50 { get; set; }
    public decimal? HeuresSupp100 { get; set; }
    public decimal? Conges { get; set; }
    public decimal? JoursFeries { get; set; }
    public decimal? TotalPrimesImposables { get; set; }
    public decimal? TotalNiExonere { get; set; }
    public decimal? BrutImposable { get; set; }

    // Cotisations salariales
    public decimal? BaseCnss { get; set; }
    public decimal? CnssRgSalarial { get; set; }
    public decimal? CnssAmoSalarial { get; set; }
    public decimal? CimrSalarial { get; set; }
    public decimal? MutuelleSalariale { get; set; }

    // IR
    public decimal? FraisProfessionnels { get; set; }
    public decimal? RevenuNetImposable { get; set; }
    public decimal? IrTaux { get; set; }
    public decimal? IR { get; set; }

    // Net
    public decimal? SalaireNetAvantArrondi { get; set; }
    public decimal? Arrondi { get; set; }
    public decimal? SalaireNet { get; set; }

    // Cotisations patronales
    public decimal? CnssRgPatronal { get; set; }
    public decimal? AmoPatronal { get; set; }
    public decimal? CimrPatronal { get; set; }
    public decimal? TotalChargesPatronales { get; set; }
    public decimal? CoutEmployeurTotal { get; set; }

    // Détails
    public List<PayrollResultPrimeDto> Primes { get; set; } = new();
    public string? ResultatJson { get; set; }
    public List<PayrollAuditStepDto>? AuditSteps { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class PayrollAuditStepDto
{
    public int StepOrder { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string FormulaDescription { get; set; } = string.Empty;
    public string? InputsJson { get; set; }
    public string? OutputsJson { get; set; }
}

public class PayrollResultPrimeDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public int Ordre { get; set; }
    public bool IsTaxable { get; set; }
}

// ════════════════════════════════════════════════════════════
// SIMULATE / BATCH
// ════════════════════════════════════════════════════════════

public class PayrollSimulateRequestDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required, Range(1, 12)]
    public int PayMonth { get; set; }

    [Required, Range(2020, 2100)]
    public int PayYear { get; set; }

    // null = mensuel ; 1 = 1-15 ; 2 = 16-31
    public int? PayHalf { get; set; }
}

public class PayrollBatchRequestDto
{
    [Required]
    public int CompanyId { get; set; }

    [Required, Range(1, 12)]
    public int PayMonth { get; set; }

    [Required, Range(2020, 2100)]
    public int PayYear { get; set; }

    /// <summary>null = tous les employés actifs de la société</summary>
    public List<int>? EmployeeIds { get; set; }

    // null = mensuel ; 1 = 1-15 ; 2 = 16-31
    public int? PayHalf { get; set; }
}

// ════════════════════════════════════════════════════════════
// SALARY PREVIEW (SalaryPreviewController)
// ════════════════════════════════════════════════════════════

public class SalaryPreviewRequestDto
{
    public decimal BaseSalary { get; set; }
    public List<SalaryPackageItemWriteDto> Items { get; set; } = new();
    public CimrConfigDto? CimrConfig { get; set; }
    public AutoRulesDto? AutoRules { get; set; }
    public int? YearsOfService { get; set; }
    public int Dependents { get; set; } = 0;

    /// <summary>
    /// Date de paie pour la résolution des règles et paramètres légaux.
    /// Défaut : aujourd'hui.
    /// </summary>
    public DateOnly? PayrollDate { get; set; }
}

// ════════════════════════════════════════════════════════════
// SUMMARY (résumé calcul complet avec breakdowns)
// ════════════════════════════════════════════════════════════

public class PayrollSummaryDto
{
    // Brut
    public decimal BaseSalary { get; set; }
    public decimal SeniorityBonus { get; set; }
    public decimal Allowances { get; set; }
    public decimal Bonuses { get; set; }
    public decimal BenefitsInKind { get; set; }
    public decimal GrossSalary { get; set; }

    // Déductions salariales
    public decimal CnssEmployee { get; set; }
    public decimal CnssBase { get; set; }
    public decimal AmoEmployee { get; set; }
    public decimal AmoBase { get; set; }
    public decimal CimrEmployee { get; set; }
    public decimal CimrBase { get; set; }
    public decimal TotalEmployeeDeductions { get; set; }

    // Base IR
    public decimal GrossTaxable { get; set; }
    public decimal ProfessionalExpenses { get; set; }
    public decimal NetTaxableIncome { get; set; }

    // IR
    public decimal IncomeTaxGross { get; set; }
    public decimal FamilyDeductions { get; set; }
    public decimal IncomeTaxNet { get; set; }

    // Net
    public decimal NetSalary { get; set; }

    // Charges patronales
    public decimal CnssEmployer { get; set; }
    public decimal AllocationsFamiliales { get; set; }
    public decimal TaxeProfessionnelle { get; set; }
    public decimal AmoEmployer { get; set; }
    public decimal CimrEmployer { get; set; }
    public decimal TotalEmployerCost { get; set; }

    // Coût total entreprise
    public decimal TotalCostToCompany { get; set; }

    // Breakdowns détaillés
    public CnssBreakdownDto? CnssBreakdown { get; set; }
    public CimrBreakdownDto? CimrBreakdown { get; set; }
    public IrBreakdownDto? IrBreakdown { get; set; }
}

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

public class CimrBreakdownDto
{
    public string Regime { get; set; } = "NONE";
    public decimal SalaireReference { get; set; }
    public decimal TauxSalarial { get; set; }
    public decimal TauxPatronal { get; set; }
    public decimal CotisationSalariale { get; set; }
    public decimal CotisationPatronale { get; set; }
}

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