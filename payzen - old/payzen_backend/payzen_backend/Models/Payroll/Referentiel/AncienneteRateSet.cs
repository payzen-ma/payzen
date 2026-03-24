namespace payzen_backend.Models.Payroll.Referentiel
{
    /// <summary>
    /// A set of seniority bonus rates (legal default or company-specific)
    ///
    /// Design:
    /// - CompanyId = null, IsLegalDefault = true → Legal minimum (created by backoffice)
    /// - CompanyId = X, IsLegalDefault = false → Company X's custom rates (cloned from legal)
    ///
    /// Versioning:
    /// - Rates are NEVER updated in place
    /// - Changes create a NEW version with new EffectiveFrom date
    /// - Old version gets EffectiveTo set to close it
    /// - This preserves historical data for payslip recalculations
    /// </summary>
    public class AncienneteRateSet
    {
        public int Id { get; set; }

        /// <summary>
        /// null = Legal default (system-wide, created by backoffice)
        /// non-null = Company-specific rates (cloned from legal default)
        /// </summary>
        public int? CompanyId { get; set; }

        /// <summary>
        /// Reference to the rate set this was cloned from (for traceability)
        /// null for legal defaults, set for company copies
        /// </summary>
        public int? ClonedFromId { get; set; }

        public required string Code { get; set; }
        public required string Name { get; set; }

        /// <summary>
        /// true = Legal minimum rates (from backoffice)
        /// false = Company-specific enhanced rates
        /// </summary>
        public bool IsLegalDefault { get; set; }

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
        public virtual Company.Company? Company { get; set; }
        public virtual AncienneteRateSet? ClonedFrom { get; set; }
        public virtual ICollection<AncienneteRateSet> ClonedTo { get; set; } = new List<AncienneteRateSet>();
        public virtual ICollection<AncienneteRate> Rates { get; set; } = new List<AncienneteRate>();

        /// <summary>
        /// Get the rate for a given number of years of seniority
        /// </summary>
        public decimal GetRateForYears(int years)
        {
            var rate = Rates
                .Where(r => years >= r.MinYears && (r.MaxYears == null || years <= r.MaxYears))
                .OrderByDescending(r => r.MinYears)
                .FirstOrDefault();

            return rate?.Rate ?? 0m;
        }

        /// <summary>
        /// Check if this rate set is currently active (based on effective dates)
        /// </summary>
        public bool IsCurrentlyActive()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return DeletedAt == null &&
                   EffectiveFrom <= today &&
                   (EffectiveTo == null || EffectiveTo >= today);
        }
    }
}
