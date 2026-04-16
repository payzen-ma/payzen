using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Referentiel;

public class LegalContractType : BaseEntity
{
    public required string Code { get; set; } // CDI, CDD, STAGE, FREELANCE
    public required string Name { get; set; }
}
