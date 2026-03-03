namespace payzen_backend.Models.Referentiel
{
    /// <summary>
    /// Statut d'un employ� (Actif, Licenci�, Retrait�, etc.)
    /// </summary>
    public class Status
    {
        public int Id { get; set; }

        // Code unique (ex: ACTIVE, FIRED, RETIRED)
        public required string Code { get; set; }

        // Libell�s multilingues
        public required string NameFr { get; set; }
        public required string NameAr { get; set; }
        public required string NameEn { get; set; }

        // Flags m�tier
        public bool IsActive { get; set; } = true;
        public bool AffectsAccess { get; set; } = false;
        public bool AffectsPayroll { get; set; } = false;
        public bool AffectsAttendance { get; set; } = false;

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }

        // Navigation
        public ICollection<Employee.Employee>? Employees { get; set; }
    }
}