using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Common.LeaveStatus;
using payzen_backend.Models.Leave;
using payzen_backend.Services.Company.Defaults.Catalog;

namespace payzen_backend.Services.Company.Defaults.Seeders
{
    public class LeaveSeeder
    {
        private readonly AppDbContext _db;

        public LeaveSeeder(AppDbContext db)
        {
            _db = db;
        }

        public async Task SeedAsync(int companyId, int userId)
        {
            // Récupérer les types de congé par défaut depuis le catalogue
            var defaultLeaves = DefaultLeaveSetup.GetDefaultLeaves();

            foreach (var leaveData in defaultLeaves)
            {
                // Vérifier si le type de congé existe déjà (par code)
                var exists = await _db.LeaveTypes
                    .AnyAsync(lt => lt.CompanyId == companyId && lt.LeaveCode == leaveData.Code && lt.DeletedAt == null);

                if (exists) continue;

                // Créer le type de congé
                var leaveType = new LeaveType
                {
                    CompanyId = companyId,
                    LeaveCode = leaveData.Code,
                    LeaveNameFr = leaveData.NameFr,
                    LeaveNameAr = leaveData.NameAr,
                    LeaveNameEn = leaveData.NameEn,
                    LeaveDescription = leaveData.Description,
                    Scope = leaveData.Scope,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                };

                _db.LeaveTypes.Add(leaveType);
                await _db.SaveChangesAsync(); // Pour obtenir l'ID

                // Créer la politique par défaut si fournie
                if (leaveData.DefaultPolicy != null)
                {
                    var policy = new LeaveTypePolicy
                    {
                        CompanyId = companyId,
                        LeaveTypeId = leaveType.Id,
                        IsEnabled = leaveData.DefaultPolicy.IsEnabled,
                        AccrualMethod = leaveData.DefaultPolicy.AccrualMethod,
                        DaysPerMonthAdult = leaveData.DefaultPolicy.DaysPerMonthAdult,
                        DaysPerMonthMinor = leaveData.DefaultPolicy.DaysPerMonthMinor,
                        RequiresBalance = leaveData.DefaultPolicy.RequiresBalance,
                        RequiresEligibility6Months = leaveData.DefaultPolicy.RequiresEligibility6Months,
                        BonusDaysPerYearAfter5Years = leaveData.DefaultPolicy.BonusDaysPerYearAfter5Years,
                        AnnualCapDays = leaveData.DefaultPolicy.AnnualCapDays,
                        AllowCarryover = leaveData.DefaultPolicy.AllowCarryover,
                        MaxCarryoverYears = leaveData.DefaultPolicy.MaxCarryoverYears,
                        MinConsecutiveDays = leaveData.DefaultPolicy.MinConsecutiveDays,
                        UseWorkingCalendar = leaveData.DefaultPolicy.UseWorkingCalendar,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = userId
                    };

                    _db.LeaveTypePolicies.Add(policy);
                }

                // Créer les règles légales si fournies
                if (leaveData.LegalRules != null && leaveData.LegalRules.Any())
                {
                    foreach (var ruleData in leaveData.LegalRules)
                    {
                        var legalRule = new LeaveTypeLegalRule
                        {
                            LeaveTypeId = leaveType.Id,
                            EventCaseCode = ruleData.EventCaseCode,
                            Description = ruleData.Description,
                            LegalArticle = ruleData.LegalArticle,
                            DaysGranted = ruleData.DaysGranted,
                            MustBeUsedWithinDays = ruleData.MustBeUsedWithinDays,
                            CanBeDiscountinuous = ruleData.CanBeDiscountinuous,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreatedBy = userId
                        };

                        _db.LeaveTypeLegalRules.Add(legalRule);
                    }
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
