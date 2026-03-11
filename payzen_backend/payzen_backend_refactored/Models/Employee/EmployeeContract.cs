namespace payzen_backend.Models.Employee
{
    /// <summary>
    /// Contrat de l'employ� avec historique
    /// </summary>
    public class EmployeeContract
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int CompanyId { get; set; }
        public int JobPositionId { get; set; }
        public int ContractTypeId { get; set; }
        public required DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ExonerationEndDate { get; set; }

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Employee? Employee { get; set; } = null!;
        public Company.Company? Company { get; set; } = null!;
        public Company.JobPosition? JobPosition { get; set; } = null!;
        public Company.ContractType? ContractType { get; set; } = null!;
        public ICollection<EmployeeSalary>? Salaries { get; set; }
    }
}