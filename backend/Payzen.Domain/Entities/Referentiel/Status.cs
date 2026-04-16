using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Referentiel;

/// <summary>Statut d'un employé (Actif, Licencié, Retraité, etc.)</summary>
public class Status : BaseEntity
{
    public required string Code { get; set; }
    public required string NameFr { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }

    public bool IsActive { get; set; } = true;
    public bool AffectsAccess { get; set; } = false;
    public bool AffectsPayroll { get; set; } = false;
    public bool AffectsAttendance { get; set; } = false;

    // Navigation
    public ICollection<Employee.Employee>? Employees { get; set; }
}
