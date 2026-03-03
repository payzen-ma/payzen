using System.Collections.Generic;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Services.Company.Defaults.Catalog
{
    /// <summary>
    /// Catalogue des types de congé et politiques par défaut pour chaque nouvelle company.
    /// Les types sont prédéfinis (congé annuel, sans solde, congés légaux) avec une politique par défaut.
    /// </summary>
    public static class DefaultLeaveSetup
    {
        public sealed class LegalRuleData
        {
            public required string EventCaseCode { get; init; }
            public required string Description { get; init; }
            public required string LegalArticle { get; init; }
            public int DaysGranted { get; init; }
            public int? MustBeUsedWithinDays { get; init; }
            public bool CanBeDiscountinuous { get; init; }
        }

        public sealed class DefaultPolicyData
        {
            public bool IsEnabled { get; init; } = true;
            public LeaveAccrualMethod AccrualMethod { get; init; }
            public decimal DaysPerMonthAdult { get; init; }
            public decimal DaysPerMonthMinor { get; init; }
            public bool RequiresBalance { get; init; }
            public bool RequiresEligibility6Months { get; init; }
            public decimal BonusDaysPerYearAfter5Years { get; init; }
            public int AnnualCapDays { get; init; }
            public bool AllowCarryover { get; init; }
            public int MaxCarryoverYears { get; init; }
            public int MinConsecutiveDays { get; init; }
            public bool UseWorkingCalendar { get; init; }
        }

        public sealed class LeaveSetupData
        {
            public required string Code { get; init; }
            public required string NameFr { get; init; }
            public required string NameAr { get; init; }
            public required string NameEn { get; init; }
            public required string Description { get; init; }
            public LeaveScope Scope { get; init; }
            public DefaultPolicyData? DefaultPolicy { get; init; }
            public IReadOnlyList<LegalRuleData>? LegalRules { get; init; }
        }

        /// <summary>
        /// Retourne les types de congé à créer pour une nouvelle entreprise (scope Company) avec politique et règles légales optionnelles.
        /// </summary>
        public static IReadOnlyList<LeaveSetupData> GetDefaultLeaves()
        {
            return new List<LeaveSetupData>
            {
                new()
                {
                    Code = "ANNUAL",
                    NameFr = "Congé annuel",
                    NameAr = "إجازة سنوية",
                    NameEn = "Annual leave",
                    Description = "Congé payé annuel selon la politique de l'entreprise",
                    Scope = LeaveScope.Company,
                    DefaultPolicy = new DefaultPolicyData
                    {
                        IsEnabled = true,
                        AccrualMethod = LeaveAccrualMethod.Monthly,
                        DaysPerMonthAdult = 1.50m,
                        DaysPerMonthMinor = 2.00m,
                        RequiresBalance = true,
                        RequiresEligibility6Months = true,
                        BonusDaysPerYearAfter5Years = 1.50m,
                        AnnualCapDays = 30,
                        AllowCarryover = true,
                        MaxCarryoverYears = 2,
                        MinConsecutiveDays = 0,
                        UseWorkingCalendar = true
                    },
                    LegalRules = null
                },
                new()
                {
                    Code = "UNPAID",
                    NameFr = "Congé sans solde",
                    NameAr = "إجازة بدون أجر",
                    NameEn = "Unpaid leave",
                    Description = "Congé sans solde accordé à la demande de l'employé",
                    Scope = LeaveScope.Company,
                    DefaultPolicy = new DefaultPolicyData
                    {
                        IsEnabled = true,
                        AccrualMethod = LeaveAccrualMethod.None,
                        DaysPerMonthAdult = 0,
                        DaysPerMonthMinor = 0,
                        RequiresBalance = false,
                        RequiresEligibility6Months = false,
                        BonusDaysPerYearAfter5Years = 0,
                        AnnualCapDays = 0,
                        AllowCarryover = false,
                        MaxCarryoverYears = 0,
                        MinConsecutiveDays = 0,
                        UseWorkingCalendar = true
                    },
                    LegalRules = null
                }
            };
        }
    }
}
