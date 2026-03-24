namespace payzen_backend.Models.Employee
{
    /// <summary>
    /// Adresse de l'employ�
    /// </summary>
    public class EmployeeAddress
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public required string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public required string ZipCode { get; set; }
        public int CityId { get; set; }

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Employee? Employee { get; set; } = null!;
        public Referentiel.City? City { get; set; } = null!;
    }
}
