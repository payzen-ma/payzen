namespace payzen_backend.Models.Payroll.Referentiel
{
    /// <summary>
    /// The exemption rule for an element under a specific authority
    /// </summary>
    public class ElementRule
    {
        public int Id { get; set; }
        public int ElementId { get; set; }
        public int AuthorityId { get; set; }
        public ExemptionType ExemptionType { get; set; }
        public string RuleDetails { get; set; } = "{}";  // NEW: JSON column for type-specific data
        public string? SourceRef { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public ElementStatus Status { get; set; } = ElementStatus.DRAFT;  // NEW: DRAFT, ACTIVE, or ARCHIVED

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation
        public virtual ReferentielElement Element { get; set; } = null!;
        public virtual Authority Authority { get; set; } = null!;

        // Rule details (0 or 1 of each, depending on ExemptionType)
        public virtual RuleCap? Cap { get; set; }
        public virtual RulePercentage? Percentage { get; set; }
        public virtual RuleFormula? Formula { get; set; }
        public virtual RuleDualCap? DualCap { get; set; }
        public virtual ICollection<RuleTier> Tiers { get; set; } = new List<RuleTier>();
        public virtual ICollection<RuleVariant> Variants { get; set; } = new List<RuleVariant>();

        /// <summary>
        /// Check if this rule is currently active
        /// </summary>
        public bool IsActive(DateOnly? asOfDate = null)
        {
            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return EffectiveFrom <= checkDate && (EffectiveTo == null || EffectiveTo >= checkDate);
        }

        /// <summary>
        /// Calculate the exemption cap for a given variant (if applicable).
        /// When variants are defined, they ARE the caps - no base cap needed.
        /// </summary>
        public decimal? GetCapForVariant(string? variantKey = null)
        {
            // If variants exist with caps, use them directly
            if (Variants.Any(v => v.OverrideCap != null))
            {
                if (variantKey != null)
                {
                    var variant = Variants.FirstOrDefault(v => v.VariantKey == variantKey);
                    return variant?.OverrideCap;
                }
                // No variant key provided but variants exist - return null (caller must specify)
                return null;
            }

            // No variants with caps - use base cap
            return Cap?.CapAmount;
        }
    }
}
