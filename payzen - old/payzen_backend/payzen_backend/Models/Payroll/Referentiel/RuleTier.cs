namespace payzen_backend.Models.Payroll.Referentiel
{
    /// <summary>
    /// Tiered exemption (e.g., first 500 = 100% exempt, next 500 = 50% exempt)
    /// </summary>
    public class RuleTier
    {
        public int Id { get; set; }
        public int RuleId { get; set; }
        public int TierOrder { get; set; }
        public decimal FromAmount { get; set; }
        public decimal? ToAmount { get; set; }
        public decimal ExemptPercent { get; set; }

        // Navigation
        public virtual ElementRule Rule { get; set; } = null!;
    }
}
