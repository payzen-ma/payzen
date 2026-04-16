using Payzen.Domain.Enums;

namespace Payzen.Application.DTOs.Payroll;

/// <summary>Réponse GET api/payroll/results (parité frontend bulletin).</summary>
public class PayrollBulletinResultsResponseDto
{
    public int Count { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public List<PayrollBulletinResultItemDto> Results { get; set; } = new();
}

public class PayrollBulletinResultItemDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public int? PayHalf { get; set; }
    public PayrollResultStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal? SalaireBase { get; set; }
    public decimal? TotalBrut { get; set; }
    public decimal? TotalCotisationsSalariales { get; set; }
    public decimal? TotalCotisationsPatronales { get; set; }
    public decimal? ImpotRevenu { get; set; }
    public decimal? TotalNet { get; set; }
    public decimal? TotalNet2 { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ClaudeModel { get; set; }
    public int? TokensUsed { get; set; }
}

/// <summary>Détail GET api/payroll/results/{id} (parité ancien PayrollController).</summary>
public class PayrollBulletinDetailDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public int? PayHalf { get; set; }
    public PayrollResultStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    public decimal? SalaireBase { get; set; }
    public decimal? HeuresSupp25 { get; set; }
    public decimal? HeuresSupp50 { get; set; }
    public decimal? HeuresSupp100 { get; set; }
    public decimal? Conges { get; set; }
    public decimal? JoursFeries { get; set; }
    public decimal? PrimeAnciennete { get; set; }

    public decimal? PrimeImposable1 { get; set; }
    public decimal? PrimeImposable2 { get; set; }
    public decimal? PrimeImposable3 { get; set; }
    public decimal? TotalPrimesImposables { get; set; }
    public decimal? TotalBrut { get; set; }

    public decimal? FraisProfessionnels { get; set; }
    public decimal? IndemniteRepresentation { get; set; }
    public decimal? PrimeTransport { get; set; }
    public decimal? PrimePanier { get; set; }
    public decimal? IndemniteDeplacement { get; set; }
    public decimal? IndemniteCaisse { get; set; }
    public decimal? PrimeSalissure { get; set; }
    public decimal? GratificationsFamilial { get; set; }
    public decimal? PrimeVoyageMecque { get; set; }
    public decimal? IndemniteLicenciement { get; set; }
    public decimal? IndemniteKilometrique { get; set; }
    public decimal? PrimeTourne { get; set; }
    public decimal? PrimeOutillage { get; set; }
    public decimal? AideMedicale { get; set; }
    public decimal? AutresPrimesNonImposable { get; set; }
    public decimal? TotalIndemnites { get; set; }
    public decimal? TotalNiExcedentImposable { get; set; }

    public decimal? CnssPartSalariale { get; set; }
    public decimal? CimrPartSalariale { get; set; }
    public decimal? AmoPartSalariale { get; set; }
    public decimal? MutuellePartSalariale { get; set; }
    public decimal? TotalCotisationsSalariales { get; set; }

    public decimal? CnssPartPatronale { get; set; }
    public decimal? CimrPartPatronale { get; set; }
    public decimal? AmoPartPatronale { get; set; }
    public decimal? MutuellePartPatronale { get; set; }
    public decimal? TotalCotisationsPatronales { get; set; }

    public decimal? ImpotRevenu { get; set; }
    public decimal? Arrondi { get; set; }
    public decimal? AvanceSurSalaire { get; set; }
    public decimal? InteretSurLogement { get; set; }
    public decimal? BrutImposable { get; set; }
    public decimal? NetImposable { get; set; }
    public decimal? TotalGains { get; set; }
    public decimal? TotalRetenues { get; set; }
    public decimal? NetAPayer { get; set; }
    public decimal? TotalNet { get; set; }
    public decimal? TotalNet2 { get; set; }

    public List<PayrollBulletinDetailPrimeDto> Primes { get; set; } = new();
    public List<PayrollBulletinAuditStepDto>? CalculationAuditSteps { get; set; }
    public List<PayrollBulletinAbsenceDto> Absences { get; set; } = new();
    public List<PayrollBulletinOvertimeDto> Overtimes { get; set; } = new();
    public List<PayrollBulletinLeaveDto> Leaves { get; set; } = new();
}

public class PayrollBulletinDetailPrimeDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public int Ordre { get; set; }
    public bool IsTaxable { get; set; }
}

public class PayrollBulletinAuditStepDto
{
    public int StepOrder { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string FormulaDescription { get; set; } = string.Empty;
    public string? InputsJson { get; set; }
    public string? OutputsJson { get; set; }
}

public class PayrollBulletinAbsenceDto
{
    public int Id { get; set; }
    public string AbsenceDate { get; set; } = string.Empty;
    public string AbsenceType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string DurationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class PayrollBulletinOvertimeDto
{
    public int Id { get; set; }
    public string OvertimeDate { get; set; } = string.Empty;
    public decimal DurationInHours { get; set; }
    public decimal RateMultiplierApplied { get; set; }
}

public class PayrollBulletinLeaveDto
{
    public int Id { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public decimal WorkingDaysDeducted { get; set; }
    public string? LeaveTypeName { get; set; }
}
