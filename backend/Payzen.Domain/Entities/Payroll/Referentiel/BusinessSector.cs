using Payzen.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Payzen.Domain.Entities.Payroll.Referentiel;

public class BusinessSector : BaseEntity
{
    [Required][MaxLength(50)]  public required string Code { get; set; }
    [Required][MaxLength(200)] public required string Name { get; set; }

    public bool IsStandard { get; set; } = false;
    public int SortOrder { get; set; } = 0;


    // Navigation
    public ICollection<Payroll.SalaryPackage> SalaryPackages { get; set; } = new List<Payroll.SalaryPackage>();
}
