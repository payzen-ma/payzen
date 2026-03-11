namespace payzen_backend.Models.Referentiel
{
    public class Nationality
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
        // Navigation properties
        public ICollection<Employee.Employee>? Employees { get; set; }
    }
}
