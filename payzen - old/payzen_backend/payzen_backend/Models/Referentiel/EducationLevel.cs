namespace payzen_backend.Models.Referentiel
{
    /// <summary>
    /// Niveau d'�ducation (Bac, Licence, Master, Doctorat, etc.)
    /// </summary>
    public class EducationLevel
    {
        public int Id { get; set; }

        // Code unique ex: BAC, BAC_2, LICENCE, MASTER, PHD
        public required string Code { get; set; }

        // Libell�s multilingues
        public required string NameFr { get; set; }
        public required string NameAr { get; set; }
        public required string NameEn { get; set; }

        // Ordre logique pour le tri (1 = niveau le plus bas, etc.)
        public int LevelOrder { get; set; }

        // Activation logique (soft/enable)
        public bool IsActive { get; set; } = true;

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }

        // Navigation
        public ICollection<Employee.Employee>? Employees { get; set; }
    }
}
