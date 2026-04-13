using Payzen.Domain.Common;
using Payzen.Domain.Enums;

namespace Payzen.Domain.Entities.Payroll;

public class PayrollResult : BaseEntity
{
    // Identification
    public int EmployeeId
    {
        get; set;
    }
    public int CompanyId
    {
        get; set;
    }
    public int Month
    {
        get; set;
    }
    public int Year
    {
        get; set;
    }

    // Période de paie.
    // null = mensuel ; 1 = 1-15 ; 2 = 16-31 (bimensuel).
    public int? PayHalf
    {
        get; set;
    }

    // Statut
    public PayrollResultStatus Status { get; set; } = PayrollResultStatus.Pending;
    public string? ErrorMessage
    {
        get; set;
    }
    public string? ResultatJson
    {
        get; set;
    }

    // Salaire de base et heures
    public decimal? SalaireBase
    {
        get; set;
    }
    public decimal? HeuresSupp25
    {
        get; set;
    }
    public decimal? HeuresSupp50
    {
        get; set;
    }
    public decimal? HeuresSupp100
    {
        get; set;
    }
    public decimal? Conges
    {
        get; set;
    }
    public decimal? JoursFeries
    {
        get; set;
    }
    public decimal? PrimeAnciennete
    {
        get; set;
    }
    public decimal? PrimeAnciennteRate
    {
        get; set;
    }

    // Primes imposables
    public decimal? PrimeImposable1
    {
        get; set;
    }
    public decimal? PrimeImposable2
    {
        get; set;
    }
    public decimal? PrimeImposable3
    {
        get; set;
    }
    public decimal? TotalPrimesImposables
    {
        get; set;
    }

    // Salaire brut
    public decimal? TotalBrut
    {
        get; set;
    }

    // Frais professionnels
    public decimal? FraisProfessionnels
    {
        get; set;
    }

    // Indemnités non imposables
    public decimal? IndemniteRepresentation
    {
        get; set;
    }
    public decimal? PrimeTransport
    {
        get; set;
    }
    public decimal? PrimePanier
    {
        get; set;
    }
    public decimal? IndemniteDeplacement
    {
        get; set;
    }
    public decimal? IndemniteCaisse
    {
        get; set;
    }
    public decimal? PrimeSalissure
    {
        get; set;
    }
    public decimal? GratificationsFamilial
    {
        get; set;
    }
    public decimal? PrimeVoyageMecque
    {
        get; set;
    }
    public decimal? IndemniteLicenciement
    {
        get; set;
    }
    public decimal? IndemniteKilometrique
    {
        get; set;
    }
    public decimal? PrimeTourne
    {
        get; set;
    }
    public decimal? PrimeOutillage
    {
        get; set;
    }
    public decimal? AideMedicale
    {
        get; set;
    }
    public decimal? AutresPrimesNonImposable
    {
        get; set;
    }
    public decimal? TotalIndemnites
    {
        get; set;
    }
    public decimal? TotalNiExcedentImposable
    {
        get; set;
    }

    // Cotisations salariales
    public decimal? CnssPartSalariale
    {
        get; set;
    }
    public decimal? CnssBase
    {
        get; set;
    }
    public decimal? CimrPartSalariale
    {
        get; set;
    }
    public decimal? CimrBase
    {
        get; set;
    }
    public decimal? AmoPartSalariale
    {
        get; set;
    }
    public decimal? AmoBase
    {
        get; set;
    }
    public decimal? MutuellePartSalariale
    {
        get; set;
    }
    public decimal? MutuelleBase
    {
        get; set;
    }
    public decimal? TotalCotisationsSalariales
    {
        get; set;
    }

    // Cotisations patronales
    public decimal? CnssPartPatronale
    {
        get; set;
    }
    public decimal? CimrPartPatronale
    {
        get; set;
    }
    public decimal? AmoPartPatronale
    {
        get; set;
    }
    public decimal? MutuellePartPatronale
    {
        get; set;
    }
    public decimal? TotalCotisationsPatronales
    {
        get; set;
    }

    // Impôt sur le revenu
    public decimal? ImpotRevenu
    {
        get; set;
    }
    public decimal? IrTaux
    {
        get; set;
    }

    // Arrondi
    public decimal? Arrondi
    {
        get; set;
    }

    // Avances et divers
    public decimal? AvanceSurSalaire
    {
        get; set;
    }
    public decimal? InteretSurLogement
    {
        get; set;
    }

    // Totaux finaux
    public decimal? BrutImposable
    {
        get; set;
    }
    public decimal? NetImposable
    {
        get; set;
    }
    public decimal? TotalGains
    {
        get; set;
    }
    public decimal? TotalRetenues
    {
        get; set;
    }
    public decimal? NetAPayer
    {
        get; set;
    }

    // Compatibilité
    public decimal? TotalNet
    {
        get; set;
    }
    public decimal? TotalNet2
    {
        get; set;
    }

    // Métadonnées
    public string? ClaudeModel
    {
        get; set;
    }
    public int? TokensUsed
    {
        get; set;
    }
    public DateTime? ProcessedAt
    {
        get; set;
    }

    // Navigation
    public Employee.Employee Employee { get; set; } = null!;
    public Company.Company Company { get; set; } = null!;
    public ICollection<PayrollResultPrime> Primes { get; set; } = new List<PayrollResultPrime>();
    public ICollection<PayrollCalculationAuditStep> CalculationAuditSteps { get; set; } = new List<PayrollCalculationAuditStep>();
}
