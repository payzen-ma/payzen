namespace payzen_backend.Models.Employee
{
    /// <summary>
    /// Salaire de base de l'employ� avec historique
    /// </summary>
    public class EmployeeSalary
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int ContractId { get; set; }
        public decimal BaseSalary { get; set; }
        public required DateTime EffectiveDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Employee? Employee { get; set; } = null!;
        public EmployeeContract? Contract { get; set; } = null!;
        public ICollection<EmployeeSalaryComponent>? Components { get; set; }
    }
}