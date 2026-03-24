using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Company;

public class CompanyDocument : BaseEntity
{

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public required string Name { get; set; }
    public required string FilePath { get; set; }
    public string? DocumentType { get; set; }
}
