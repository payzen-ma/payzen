using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Referentiel;

/// <summary>Niveau d'éducation (Bac, Licence, Master, Doctorat, etc.)</summary>
public class EducationLevel : BaseEntity
{
    public required string Code { get; set; }
    public required string NameFr { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public int LevelOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Employee.Employee>? Employees { get; set; }
}
