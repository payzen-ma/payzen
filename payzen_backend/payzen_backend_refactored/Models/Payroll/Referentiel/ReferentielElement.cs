using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Payroll.Referentiel
{
    /// <summary>
    /// A compensation element in the référentiel library (Transport, Panier, Représentation, etc.)
    /// </summary>
    public class ReferentielElement
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string? Code { get; set; }  // Optional unique code for reference elements (e.g., 'transport_domicile')
        public required string Name { get; set; }
        public int CategoryId { get; set; }
        public string? Description { get; set; }
        public PaymentFrequency DefaultFrequency { get; set; }
        public ElementStatus Status { get; set; } = ElementStatus.DRAFT;  // NEW: DRAFT, ACTIVE, or ARCHIVED
        public bool HasConvergence { get; set; }  // NEW: Calculated field indicating CNSS/DGI alignment
        public bool IsActive { get; set; } = true;

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation
        public virtual ElementCategory Category { get; set; } = null!;
        public virtual ICollection<ElementRule> Rules { get; set; } = new List<ElementRule>();

        /// <summary>
        /// Get the rule for a specific authority (CNSS, IR, etc.)
        /// </summary>
        public ElementRule? GetRuleForAuthority(string authorityCode, DateOnly? asOfDate = null)
        {
            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return Rules.FirstOrDefault(r =>
                r.Authority.Code == authorityCode &&
                r.EffectiveFrom <= checkDate &&
                (r.EffectiveTo == null || r.EffectiveTo >= checkDate));
        }

        /// <summary>
        /// Check if CNSS and IR rules are in convergence (same treatment)
        /// </summary>
        public bool IsConvergence(DateOnly? asOfDate = null)
        {
            var cnssRule = GetRuleForAuthority("CNSS", asOfDate);
            var irRule = GetRuleForAuthority("IR", asOfDate);

            if (cnssRule == null || irRule == null) return false;

            // Compare exemption types
            if (cnssRule.ExemptionType != irRule.ExemptionType) return false;

            // Compare type-specific details
            switch (cnssRule.ExemptionType)
            {
                case ExemptionType.CAPPED:
                case ExemptionType.PERCENTAGE_CAPPED:
                case ExemptionType.FORMULA_CAPPED:
                    if (cnssRule.Cap?.CapAmount != irRule.Cap?.CapAmount) return false;
                    if (cnssRule.Cap?.CapUnit != irRule.Cap?.CapUnit) return false;
                    break;

                case ExemptionType.PERCENTAGE:
                    if (cnssRule.Percentage?.Percentage != irRule.Percentage?.Percentage) return false;
                    if (cnssRule.Percentage?.EligibilityId != irRule.Percentage?.EligibilityId) return false;
                    if (cnssRule.Percentage?.BaseReference != irRule.Percentage?.BaseReference) return false;
                    break;

                case ExemptionType.FORMULA:
                    if (cnssRule.Formula?.Multiplier != irRule.Formula?.Multiplier) return false;
                    if (cnssRule.Formula?.ParameterId != irRule.Formula?.ParameterId) return false;
                    if (cnssRule.Formula?.ResultUnit != irRule.Formula?.ResultUnit) return false;
                    break;

                case ExemptionType.TIERED:
                    if (!CompareTiers(cnssRule.Tiers, irRule.Tiers)) return false;
                    break;

                case ExemptionType.DUAL_CAP:
                    if (cnssRule.DualCap?.FixedCapAmount != irRule.DualCap?.FixedCapAmount) return false;
                    if (cnssRule.DualCap?.FixedCapUnit != irRule.DualCap?.FixedCapUnit) return false;
                    if (cnssRule.DualCap?.PercentageCap != irRule.DualCap?.PercentageCap) return false;
                    if (cnssRule.DualCap?.BaseReference != irRule.DualCap?.BaseReference) return false;
                    if (cnssRule.DualCap?.Logic != irRule.DualCap?.Logic) return false;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Compare two tier collections for equality
        /// </summary>
        private static bool CompareTiers(ICollection<RuleTier> tiers1, ICollection<RuleTier> tiers2)
        {
            if (tiers1.Count != tiers2.Count) return false;

            var sorted1 = tiers1.OrderBy(t => t.TierOrder).ToList();
            var sorted2 = tiers2.OrderBy(t => t.TierOrder).ToList();

            for (int i = 0; i < sorted1.Count; i++)
            {
                if (sorted1[i].FromAmount != sorted2[i].FromAmount) return false;
                if (sorted1[i].ToAmount != sorted2[i].ToAmount) return false;
                if (sorted1[i].ExemptPercent != sorted2[i].ExemptPercent) return false;
            }

            return true;
        }
    }
}
