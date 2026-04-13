using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Referentiel;

/// <summary>Genre de l'employé (Homme, Femme, Autre)</summary>
public class Gender : BaseEntity
{
    public required string Code
    {
        get; set;
    }
    public required string NameFr
    {
        get; set;
    }
    public required string NameAr
    {
        get; set;
    }
    public required string NameEn
    {
        get; set;
    }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Employee.Employee>? Employees
    {
        get; set;
    }
}
