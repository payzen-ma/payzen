namespace payzen_backend.Models.Employee
{
    public class EmployeeChild
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required DateTime DateOfBirth { get; set; }
        public int? GenderId { get; set; }
        public bool IsDependent { get; set; } = true;
        public bool IsStudent { get; set; } = false;

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Employee Employee { get; set; } = null!;
        public Referentiel.Gender? Gender { get; set; }
    }
}
