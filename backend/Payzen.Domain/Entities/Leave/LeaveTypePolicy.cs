using System.ComponentModel.DataAnnotations.Schema;
using Payzen.Domain.Common;
using Payzen.Domain.Enums;

namespace Payzen.Domain.Entities.Leave;

public class LeaveTypePolicy : BaseEntity
{
    public int? CompanyId { get; set; }
    public Company.Company? Company { get; set; }

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public bool IsEnabled { get; set; } = true;

    // Balance behavior
    public bool RequiresBalance { get; set; } = true;
    public bool AllowNegativeBalance { get; set; } = false;

    // Pay behavior
    public bool IsPaid { get; set; } = true;

    // Eligibility
    public bool RequiresEligibility6Months { get; set; } = false;

    // Accrual
    public LeaveAccrualMethod AccrualMethod { get; set; } = LeaveAccrualMethod.None;

    [Column(TypeName = "decimal(5,2)")]
    public decimal DaysPerMonthAdult { get; set; } = 1.50m;

    [Column(TypeName = "decimal(5,2)")]
    public decimal DaysPerMonthMinor { get; set; } = 2.00m;

    [Column(TypeName = "decimal(5,2)")]
    public decimal BonusDaysPerYearAfter5Years { get; set; } = 0m;

    public int AnnualCapDays { get; set; } = 0;
    public bool AllowCarryover { get; set; } = false;
    public int MaxCarryoverYears { get; set; } = 0;
    public int MinConsecutiveDays { get; set; } = 0;
    public bool UseWorkingCalendar { get; set; } = true;

    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}
