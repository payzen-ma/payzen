namespace payzen_backend.Models.Employee
{
    /// <summary>
    /// Documents de l'employ� (CIN, Passeport, Dipl�me, etc.)
    /// </summary>
    public class EmployeeDocument
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public required string Name { get; set; }
        public required string FilePath { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public required string DocumentType { get; set; }

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Employee? Employee { get; set; } = null!;
    }
}
