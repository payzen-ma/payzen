namespace payzen_backend.Models.Payroll.Referentiel
{
    /// <summary>
    /// Category for compensation elements (Indemnité professionnelle, sociale, etc.)
    /// </summary>
    public class ElementCategory
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        /// <summary>When false, category cannot be used for new elements (Phase 1.2).</summary>
        public bool IsActive { get; set; } = true;

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation
        public virtual ICollection<ReferentielElement> Elements { get; set; } = new List<ReferentielElement>();
    }
}
