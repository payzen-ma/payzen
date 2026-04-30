using Payzen.Domain.Common;
using Payzen.Domain.Enums;

namespace Payzen.Domain.Entities.Payroll;

public class PayrollTaxSnapshot : BaseEntity
{
    public int PayrollResultId  { get; set; }
    public int EmployeeId { get; set; }
    public int CompanyId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    // Cumuls depuis janvier
    public decimal CumulBrut { get; set; }
    public decimal CumulCnss { get; set; }
    public decimal CumulAmo { get; set; }
    public decimal CumulSni { get; set; } // Salaire net imposable cumulé
    public decimal CumulIr { get; set; } // Impôt sur le revenu cumulé
    public decimal CumulNet { get; set; } // Net à payercumulé

    // Taux effectif
    public decimal TauxEffectif { get; set; }

    // Navigation
    public PayrollResult PayrollResult { get; set; } = null!;
}