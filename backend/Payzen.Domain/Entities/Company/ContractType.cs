using Payzen.Domain.Common;
using Payzen.Domain.Entities.Referentiel;

namespace Payzen.Domain.Entities.Company;

public class ContractType : BaseEntity
{
    public required string ContractTypeName { get; set; }
    public int CompanyId { get; set; }

    public int? LegalContractTypeId { get; set; }
    public int? StateEmploymentProgramId { get; set; }

    // Navigation
    public Company? Company { get; set; } = null!;
    public ICollection<Employee.EmployeeContract>? Employees { get; set; }
    public LegalContractType? LegalContractType { get; set; }
    public StateEmploymentProgram? StateEmploymentProgram { get; set; }
}
