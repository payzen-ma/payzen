using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Referentiel;

/// <summary>Statut marital (Célibataire, Marié(e), Divorcé(e), Veuf/Veuve)</summary>
public class MaritalStatus : BaseEntity
{
    public required string Code { get; set; }
    public required string NameFr { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }

    // Navigation properties
    public ICollection<Employee.Employee>? Employees { get; set; }
}
