namespace payzen_backend.Models.Payroll.Referentiel
{
    /// <summary>
    /// Defines who is eligible for certain exemptions (All, Cadres supérieurs, PDG/DG, etc.)
    /// </summary>
    public class EligibilityCriteria
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation
        public virtual ICollection<RulePercentage> RulePercentages { get; set; } = new List<RulePercentage>();
        public virtual ICollection<RuleVariant> RuleVariants { get; set; } = new List<RuleVariant>();
    }
}
