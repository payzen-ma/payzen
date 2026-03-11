using payzen_backend.Models.Referentiel;

namespace payzen_backend.Models.Company
{
    public class ContractType
    {
        public int Id { get; set; }
        public required string ContractTypeName { get; set; }
        public int CompanyId { get; set; }

        // conformité marocaine (non libre)
        // Nullable pour le moment, devien obligatioir apres seed values
        public int? LegalContractTypeId { get; set; }
        public int? StateEmploymentProgramId { get; set; } // NONE / ANAPEC / IDMAJ / TAHFIZ

        // Audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation
        public Company? Company { get; set; } = null!;
        public ICollection<Employee.EmployeeContract>? Employees { get; set; }

        // NOUVEAU: navigation vers référentiels
        public LegalContractType? LegalContractType { get; set; }
        public StateEmploymentProgram? StateEmploymentProgram { get; set; }
    }
}
