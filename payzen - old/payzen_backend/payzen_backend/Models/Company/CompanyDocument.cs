using System;

namespace payzen_backend.Models.Company
{
    public class CompanyDocument
    {
        public int Id { get; set; }

        // relation
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        // meta fichier
        public required string Name { get; set; }          // nom lisible, ex: "logo_principal.png"
        public required string FilePath { get; set; }      // chemin relatif/URL vers le fichier
        public string? DocumentType { get; set; }          // ex: "logo", "statuts", "contrat"

        // audit / soft-delete simple
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}
