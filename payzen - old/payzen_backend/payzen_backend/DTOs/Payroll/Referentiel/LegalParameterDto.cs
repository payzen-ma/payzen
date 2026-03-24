namespace payzen_backend.DTOs.Payroll.Referentiel
{
    /// <summary>
    /// DTO for Legal Parameter (SMIG, SMAG, etc.)
    /// </summary>
    public class LegalParameterDto
    {
        public int Id { get; set; }
        /// <summary>Immutable machine-readable key for lookups (e.g. "CNSS_PLAFOND").</summary>
        public string Code { get; set; } = string.Empty;
        /// <summary>Display name (from Label in DB). Exposed as "name" for frontend.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Description (from Source in DB). Exposed as "description" for frontend.</summary>
        public string? Description { get; set; }
        public string? Source { get; set; }
        public decimal Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public bool IsActive { get; set; }  // Computed from IsActive() method
    }

    /// <summary>
    /// DTO for creating/updating Legal Parameter
    /// </summary>
    public class CreateLegalParameterDto
    {
        /// <summary>Optional immutable code. Auto-generated from Name if not provided.</summary>
        public string? Code { get; set; }
        /// <summary>Display name (stored as Label in DB). API accepts "name" from frontend.</summary>
        public required string Name { get; set; }
        /// <summary>Description (stored as Source in DB). API accepts "description" from frontend.</summary>
        public string? Description { get; set; }
        public string? Source { get; set; }
        public decimal Value { get; set; }
        public required string Unit { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }
}
