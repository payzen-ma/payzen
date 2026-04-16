using System.ComponentModel.DataAnnotations.Schema;
using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Leave;

public class LeaveBalance : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee.Employee Employee { get; set; } = null!;

    public int CompanyId { get; set; }
    public Company.Company Company { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    /// <summary>Année du solde (période mensuelle).</summary>
    public int Year { get; set; }

    /// <summary>Mois du solde (1-12).</summary>
    public int Month { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal OpeningDays { get; set; } = 0m;

    [Column(TypeName = "decimal(10,2)")]
    public decimal AccruedDays { get; set; } = 0m;

    [Column(TypeName = "decimal(10,2)")]
    public decimal UsedDays { get; set; } = 0m;

    [Column(TypeName = "decimal(10,2)")]
    public decimal CarryInDays { get; set; } = 0m;

    [Column(TypeName = "decimal(10,2)")]
    public decimal CarryOutDays { get; set; } = 0m;

    [Column(TypeName = "decimal(10,2)")]
    public decimal ClosingDays { get; set; } = 0m;

    public DateOnly? CarryoverExpiresOn { get; set; }
    public DateTimeOffset? LastRecalculatedAt { get; set; }

    /// <summary>Années après la fin du mois avant expiration du solde (règle métier standard).</summary>
    public const int BalanceValidityYears = 2;

    /// <summary>Date d'expiration du solde : dernier jour du mois (Year, Month) + <see cref="BalanceValidityYears"/> ans.</summary>
    public DateOnly GetBalanceExpiresOn()
    {
        var lastDayOfMonth = new DateOnly(Year, Month, DateTime.DaysInMonth(Year, Month));
        return lastDayOfMonth.AddYears(BalanceValidityYears);
    }
}
