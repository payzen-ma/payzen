namespace payzen_backend.Models.Payroll.Dtos
{
    /// <summary>
    /// CIMR (Caisse Interprofessionnelle Marocaine de Retraite) configuration
    /// Matches frontend CimrConfig interface
    /// </summary>
    public class CimrConfigDto
    {
        /// <summary>
        /// CIMR regime type: AL_KAMIL, AL_MOUNASSIB, or NONE
        /// - AL_KAMIL: Standard regime with rates 3%-10%
        /// - AL_MOUNASSIB: PME regime with rates 6%-12% (capped at CNSS ceiling)
        /// - NONE: Not enrolled in CIMR
        /// </summary>
        public string Regime { get; set; } = "NONE";

        /// <summary>
        /// Employee contribution rate (taux salarial)
        /// Al Kamil: 3% to 10%
        /// Al Mounassib: 6% to 12%
        /// </summary>
        public decimal EmployeeRate { get; set; }

        /// <summary>
        /// Employer contribution rate (taux patronal)
        /// Typically employeeRate × 1.3
        /// </summary>
        public decimal EmployerRate { get; set; }

        /// <summary>
        /// Optional custom employer rate override
        /// If null, uses standard calculation (employeeRate × 1.3)
        /// </summary>
        public decimal? CustomEmployerRate { get; set; }
    }
}
