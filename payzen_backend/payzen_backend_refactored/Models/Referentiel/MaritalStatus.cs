namespace payzen_backend.Models.Referentiel
{
    /// <summary>
    /// Statut marital (C�libataire, Mari�(e), Divorc�(e), Veuf/Veuve)
    /// </summary>
    public class MaritalStatus
    {
        public int Id { get; set; }

        // Code unique (ex : SINGLE, MARRIED, DIVORCED, WIDOW)
        public required string Code { get; set; }

        // Libell�s multilingues
        public required string NameFr { get; set; }
        public required string NameAr { get; set; }
        public required string NameEn { get; set; }

        // Activation logique
        public bool IsActive { get; set; } = true;

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }

        // Navigation properties
        public ICollection<Employee.Employee>? Employees { get; set; }
    }
}