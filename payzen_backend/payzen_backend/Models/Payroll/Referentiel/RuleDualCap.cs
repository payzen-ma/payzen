namespace payzen_backend.Models.Payroll.Referentiel
{
    /// <summary>
    /// Dual cap rule: combines a fixed cap AND a percentage cap.
    /// Used for rules like DGI ticket-restaurant (20 DH/jour ET 20% du SBI).
    /// The exemption is limited by BOTH conditions.
    /// </summary>
    public class RuleDualCap
    {
        public int Id { get; set; }
        public int RuleId { get; set; }

        // Fixed cap portion (e.g., 20 DH/jour)
        public decimal FixedCapAmount { get; set; }
        public CapUnit FixedCapUnit { get; set; }

        // Percentage cap portion (e.g., 20% du SBI)
        /// <summary>PercentageCap stored as human % (20 = 20%). Divided by 100 in calculations.</summary>
        public decimal PercentageCap { get; set; }
        public BaseReference BaseReference { get; set; }

        // How to combine the two caps (MIN = most restrictive, MAX = most favorable)
        public DualCapLogic Logic { get; set; } = DualCapLogic.MIN;

        // Navigation
        public virtual ElementRule Rule { get; set; } = null!;

        /// <summary>
        /// Calculate the effective cap given a base amount (salary).
        /// Returns the appropriate cap based on the Logic (MIN or MAX).
        /// </summary>
        public decimal CalculateEffectiveCap(decimal baseAmount, int periodsInUnit = 1)
        {
            var fixedCap = FixedCapAmount * periodsInUnit;
            // PercentageCap is stored as human % (20 = 20%), divide by 100
            var percentageCap = baseAmount * (PercentageCap / 100);

            return Logic == DualCapLogic.MIN
                ? Math.Min(fixedCap, percentageCap)
                : Math.Max(fixedCap, percentageCap);
        }
    }
}
