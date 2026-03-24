namespace payzen_backend.Models.Referentiel
{
    public class LegalContractType
    {
        public int Id { get; set; }
        public required string Code { get; set; } // CDI, CDD, STAGE, FREELANCE
        public required string Name { get; set; } // Libellé

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? DeletedBy { get; set; }
    }
}
