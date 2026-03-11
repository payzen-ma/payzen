using payzen_backend.Models.Company;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Payroll;

namespace payzen_backend.Models.Payroll
{
    public class PayrollResult
    {
        public int Id { get; set; }

        // ========== Identification ==========
        public int EmployeeId { get; set; }
        public int CompanyId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }

        // ========== Statut du traitement ==========
        public PayrollResultStatus Status { get; set; } = PayrollResultStatus.Pending;
        public string? ErrorMessage { get; set; }

        // ========== Résultat Claude ==========
        public string? ResultatJson { get; set; }   // JSON brut retourné par Claude

        // ========== Montants extraits du JSON (pour requêtes/reporting sans parser le JSON) ==========

        // SALAIRE DE BASE ET HEURES
        public decimal? SalaireBase { get; set; }
        public decimal? HeuresSupp25 { get; set; }
        public decimal? HeuresSupp50 { get; set; }
        public decimal? HeuresSupp100 { get; set; }
        public decimal? Conges { get; set; }
        public decimal? JoursFeries { get; set; }
        public decimal? PrimeAnciennete { get; set; }
        public decimal? PrimeAnciennteRate { get; set; }

        // PRIMES IMPOSABLES
        public decimal? PrimeImposable1 { get; set; }
        public decimal? PrimeImposable2 { get; set; }
        public decimal? PrimeImposable3 { get; set; }
        public decimal? TotalPrimesImposables { get; set; }

        // 📈 SALAIRE BRUT
        public decimal? TotalBrut { get; set; }

        // 🏢 FRAIS PROFESSIONNELS
        public decimal? FraisProfessionnels { get; set; }

        // 🎁 INDEMNITES NON IMPOSABLES
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
        /// <summary>Partie imposable des indemnités (excédent au-delà des plafonds DGI).</summary>
        public decimal? TotalNiExcedentImposable { get; set; }

        // 🔴 COTISATIONS SALARIALES
        public decimal? CnssPartSalariale { get; set; }
        public decimal? CnssBase { get; set; }
        public decimal? CimrPartSalariale { get; set; }
        public decimal? CimrBase { get; set; }
        public decimal? AmoPartSalariale { get; set; }
        public decimal? AmoBase { get; set; }
        public decimal? MutuellePartSalariale { get; set; }
        public decimal? MutuelleBase { get; set; }
        public decimal? TotalCotisationsSalariales { get; set; }

        // 🔵 COTISATIONS PATRONALES
        public decimal? CnssPartPatronale { get; set; }
        public decimal? CimrPartPatronale { get; set; }
        public decimal? AmoPartPatronale { get; set; }
        public decimal? MutuellePartPatronale { get; set; }
        public decimal? TotalCotisationsPatronales { get; set; }

        // IMPOT SUR LE REVENU
        public decimal? ImpotRevenu { get; set; }
        public decimal? IrTaux { get; set; }

        // ARRONDI
        public decimal? Arrondi { get; set; }

        // AVANCES ET DIVERS
        public decimal? AvanceSurSalaire { get; set; }
        public decimal? InteretSurLogement { get; set; }

        // TOTAUX FINAUX
        public decimal? BrutImposable { get; set; }
        public decimal? NetImposable { get; set; }
        public decimal? TotalGains { get; set; }
        public decimal? TotalRetenues { get; set; }
        public decimal? NetAPayer { get; set; }

        // ANCIENS CHAMPS (compatibilité)
        public decimal? TotalNet { get; set; }
        public decimal? TotalNet2 { get; set; }

        // ========== Métadonnées ==========
        public string? ClaudeModel { get; set; }       // "claude-3-sonnet-20240229"
        public int? TokensUsed { get; set; }
        public DateTime? ProcessedAt { get; set; }

        // ========== Audit ==========
        public DateTimeOffset CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // ========== Navigation ==========
        public Employee.Employee Employee { get; set; }
        public Company.Company Company { get; set; }

        /// <summary>
        /// Liste dynamique de toutes les primes (imposables et non imposables)
        /// Permet de gérer un nombre illimité de primes par fiche de paie
        /// </summary>
        public ICollection<PayrollResultPrime> Primes { get; set; } = new List<PayrollResultPrime>();

        /// <summary>
        /// Audit trail du calcul : un enregistrement par module (formule + entrées/sorties).
        /// </summary>
        public ICollection<PayrollCalculationAuditStep> CalculationAuditSteps { get; set; } = new List<PayrollCalculationAuditStep>();
    }

    public enum PayrollResultStatus
    {
        Pending = 0,
        Processing = 1,
        OK = 2,
        Error = 3,
        ManualReviewRequired = 4
    }

}