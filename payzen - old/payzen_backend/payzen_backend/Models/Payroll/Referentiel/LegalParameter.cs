namespace payzen_backend.Models.Payroll.Referentiel
{
    /// <summary>
    /// Legal parameters that can change over time (SMIG, SMAG, etc.)
    /// Rate parameters are stored as ratios (0.0448 = 4.48%). Multiplied directly in calculations.
    /// </summary>
    public class LegalParameter
    {
        public int Id { get; set; }
        /// <summary>
        /// Immutable machine-readable key used for lookups (e.g. "CNSS_PLAFOND").
        /// Set once at creation; never updated afterwards.
        /// </summary>
        public required string Code { get; set; }
        public required string Label { get; set; }
        public decimal Value { get; set; }
        public required string Unit { get; set; }
        public string? Source { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation
        public virtual ICollection<RuleFormula> RuleFormulas { get; set; } = new List<RuleFormula>();

        /// <summary>
        /// Check if this parameter is currently active
        /// </summary>
        public bool IsActive(DateOnly? asOfDate = null)
        {
            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return EffectiveFrom <= checkDate && (EffectiveTo == null || EffectiveTo >= checkDate);
        }
    }
}
