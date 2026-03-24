namespace payzen_backend.Models.Company
{
    /// <summary>
    /// Poste de travail par soci�t� (D�veloppeur, Manager, RH, etc.)
    /// </summary>
    public class JobPosition
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int CompanyId { get; set; }

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Company? Company { get; set; } = null!;
        public ICollection<Employee.EmployeeContract>? EmployeeContracts { get; set; }
    }
}
