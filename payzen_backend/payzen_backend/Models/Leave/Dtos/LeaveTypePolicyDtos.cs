using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Models.Leave.Dtos
{
    public class LeaveTypePolicyCreateDto
    {
        // Nullable: null = global policy
        public int? CompanyId { get; set; }

        [Required]
        public int LeaveTypeId { get; set; }

        public bool IsEnabled { get; set; } = true;

        public LeaveAccrualMethod AccrualMethod { get; set; } = LeaveAccrualMethod.Monthly;

        public decimal DaysPerMonthAdult { get; set; } = 1.50m;
        public decimal DaysPerMonthMinor { get; set; } = 2.00m;

        public decimal BonusDaysPerYearAfter5Years { get; set; } = 1.50m;

        public bool RequiresEligibility6Months { get; set; } = false;
        public bool RequiresBalance { get; set; }

        public int AnnualCapDays { get; set; } = 30;
        public bool AllowCarryover { get; set; } = true;
        public int MaxCarryoverYears { get; set; } = 2;
        public int MinConsecutiveDays { get; set; } = 12;

        public bool UseWorkingCalendar { get; set; } = true;
    }

    public class LeaveTypePolicyPatchDto
    {
        public bool? IsEnabled { get; set; }

        public LeaveAccrualMethod? AccrualMethod { get; set; }

        public decimal? DaysPerMonthAdult { get; set; }
        public decimal? DaysPerMonthMinor { get; set; }

        public decimal? BonusDaysPerYearAfter5Years { get; set; }
        public bool? RequiresEligibility6Months { get; set; }

        public bool? RequiresBalance { get; set; }
        public int? AnnualCapDays { get; set; }
        public bool? AllowCarryover { get; set; }
        public int? MaxCarryoverYears { get; set; }
        public int? MinConsecutiveDays { get; set; }

        public bool? UseWorkingCalendar { get; set; }
    }

    public class LeaveTypePolicyReadDto
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int LeaveTypeId { get; set; }

        public bool IsEnabled { get; set; }
        public LeaveAccrualMethod AccrualMethod { get; set; }

        public decimal DaysPerMonthAdult { get; set; }
        public decimal DaysPerMonthMinor { get; set; }

        public bool RequiresEligibility6Months { get; set; }
        public bool RequiresBalance { get; set; }

        public decimal BonusDaysPerYearAfter5Years { get; set; }

        public int AnnualCapDays { get; set; }
        public bool AllowCarryover { get; set; }
        public int MaxCarryoverYears { get; set; }
        public int MinConsecutiveDays { get; set; }

        public bool UseWorkingCalendar { get; set; }
    }
}