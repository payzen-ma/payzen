namespace payzen_backend.Models.Referentiel
{
    public class StateEmploymentProgram
    {
        public int Id { get; set; }
        public required string Code { get; set; } // NONE, ANAPEC, IDMAJ, TAHFIZ
        public required string Name { get; set; }

        // Règles légales
        public bool IsIrExempt { get; set; }
        public bool IsCnssEmployeeExempt { get; set; }
        public bool IsCnssEmployerExempt { get; set; }

        public int? MaxDurationMonths { get; set; }
        public decimal? SalaryCeiling { get; set; }

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? DeletedBy { get; set; }
    }
}
