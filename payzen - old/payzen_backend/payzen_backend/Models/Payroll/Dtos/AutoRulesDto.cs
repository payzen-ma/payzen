namespace payzen_backend.Models.Payroll.Dtos
{
    /// <summary>
    /// Auto-calculated rules based on Moroccan labor law (Morocco 2025)
    /// Matches frontend AutoRules interface
    /// </summary>
    public class AutoRulesDto
    {
        /// <summary>
        /// Enable automatic seniority bonus calculation (Prime d'ancienneté)
        /// Rates: 5% (2-5y), 10% (5-12y), 15% (12-20y), 20% (20+y)
        /// </summary>
        public bool SeniorityBonusEnabled { get; set; } = true;

        /// <summary>
        /// Regulation version for the auto rules
        /// </summary>
        public string RuleVersion { get; set; } = "MA_2025";
    }
}
