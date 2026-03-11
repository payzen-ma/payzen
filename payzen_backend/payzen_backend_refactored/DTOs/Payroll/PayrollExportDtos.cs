namespace payzen_backend.DTOs.Payroll
{
    /// <summary>
    /// Ligne du Journal de Paie (export mensuel complet)
    /// </summary>
    public class JournalPaieRow
    {
        public string Matricule { get; set; } = string.Empty;
        public string NomPrenom { get; set; } = string.Empty;
        public string CIN { get; set; } = string.Empty;
        public string CNSS { get; set; } = string.Empty;
        public decimal SalaireBase { get; set; }
        public decimal TotalBrut { get; set; }
        public decimal CotisationsSalariales { get; set; }
        public decimal IR { get; set; }
        public decimal NetAPayer { get; set; }
        /// <summary>Détail des primes séparé par " | " ex: "Prime excellence: 500,00 | Prime transport: 200,00"</summary>
        public string DetailsPrimes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ligne de l'État CNSS (format Damancom)
    /// </summary>
    public class EtatCnssRow
    {
        public string NomPrenom { get; set; } = string.Empty;
        public string NumeroCnss { get; set; } = string.Empty;
        public decimal SalaireBrutDeclare { get; set; }
        /// <summary>Nombre de jours déclarés (26 par défaut)</summary>
        public int NombreJoursDeclare { get; set; } = 26;
    }

    /// <summary>
    /// Ligne de l'État IR (Impôt sur le Revenu)
    /// </summary>
    public class EtatIrRow
    {
        public string NomPrenom { get; set; } = string.Empty;
        public string CIN { get; set; } = string.Empty;
        public string CNSS { get; set; } = string.Empty;
        public decimal BrutImposable { get; set; }
        public decimal IRRetenu { get; set; }
    }
}
