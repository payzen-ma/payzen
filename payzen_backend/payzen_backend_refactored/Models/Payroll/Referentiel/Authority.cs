namespace payzen_backend.Models.Payroll.Referentiel
{
    /// <summary>
    /// Represents a regulatory authority (CNSS, IR, AMO, CIMR, etc.)
    /// </summary>
    public class Authority
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        /// <summary>When false, authority cannot be used for new rules (Phase 1.2).</summary>
        public bool IsActive { get; set; } = true;

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation
        public virtual ICollection<ElementRule> ElementRules { get; set; } = new List<ElementRule>();
    }
}
