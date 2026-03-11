using System.ComponentModel.DataAnnotations.Schema;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Models.Leave
{
    public class LeaveTypePolicy
    {
        public int Id { get; set; }

        // Nullable pour supporter les policies globales (CompanyId == null)
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

        // Utiliser int pour AnnualCapDays pour correspondre aux DTOs/contr�leur
        public int AnnualCapDays { get; set; } = 0;

        public bool AllowCarryover { get; set; } = false;
        public int MaxCarryoverYears { get; set; } = 0;

        public int MinConsecutiveDays { get; set; } = 0;
        public bool UseWorkingCalendar { get; set; } = true;

        // Versioning/Validit� optionnelle
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }

        // Audit / soft delete
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}