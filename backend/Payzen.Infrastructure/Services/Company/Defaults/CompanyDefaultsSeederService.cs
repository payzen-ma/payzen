using Microsoft.EntityFrameworkCore;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Company;
using Payzen.Domain.Entities.Leave;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Company.Defaults;

/// <summary>
/// Seed des données par défaut lors de la création d'une entreprise.
/// Idempotent — ne recrée pas ce qui existe déjà.
/// Miroir de CompanyDefaultsSeeder du monolithe.
/// </summary>
public class CompanyDefaultsSeederService : ICompanyDefaultsSeeder
{
    private readonly AppDbContext _db;

    public CompanyDefaultsSeederService(AppDbContext db) => _db = db;

    public async Task SeedDefaultsAsync(int companyId, int userId, CancellationToken ct = default)
    {
        await SeedContractTypesAsync(companyId, userId, ct);
        await SeedDepartmentsAsync(companyId, userId, ct);
        await SeedJobPositionsAsync(companyId, userId, ct);
        await SeedEmployeeCategoriesAsync(companyId, userId, ct);
        await SeedWorkingCalendarAsync(companyId, userId, ct);
        await SeedLeaveTypesAsync(companyId, userId, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedContractTypesAsync(int companyId, int userId, CancellationToken ct)
    {
        if (await _db.ContractTypes.AnyAsync(c => c.CompanyId == companyId, ct))
            return;
        var defaults = new[] { "CDI", "CDD", "Intérim", "Stage", "Freelance" };
        foreach (var name in defaults)
            _db.ContractTypes.Add(
                new ContractType
                {
                    CompanyId = companyId,
                    ContractTypeName = name,
                    CreatedBy = userId,
                }
            );
    }

    private async Task SeedDepartmentsAsync(int companyId, int userId, CancellationToken ct)
    {
        if (await _db.Departements.AnyAsync(d => d.CompanyId == companyId, ct))
            return;
        var defaults = new[]
        {
            "Direction",
            "Ressources Humaines",
            "Comptabilité",
            "Commercial",
            "Informatique",
            "Production",
        };
        foreach (var name in defaults)
            _db.Departements.Add(
                new Departement
                {
                    CompanyId = companyId,
                    DepartementName = name,
                    CreatedBy = userId,
                }
            );
    }

    private async Task SeedJobPositionsAsync(int companyId, int userId, CancellationToken ct)
    {
        if (await _db.JobPositions.AnyAsync(j => j.CompanyId == companyId, ct))
            return;
        var defaults = new[]
        {
            "Directeur Général",
            "Responsable RH",
            "Comptable",
            "Commercial",
            "Développeur",
            "Technicien",
        };
        foreach (var name in defaults)
            _db.JobPositions.Add(
                new JobPosition
                {
                    CompanyId = companyId,
                    Name = name,
                    CreatedBy = userId,
                }
            );
    }

    private async Task SeedEmployeeCategoriesAsync(int companyId, int userId, CancellationToken ct)
    {
        if (await _db.EmployeeCategories.AnyAsync(c => c.CompanyId == companyId, ct))
            return;
        _db.EmployeeCategories.Add(
            new Domain.Entities.Employee.EmployeeCategory
            {
                CompanyId = companyId,
                Name = "Cadre",
                CreatedBy = userId,
            }
        );
        _db.EmployeeCategories.Add(
            new Domain.Entities.Employee.EmployeeCategory
            {
                CompanyId = companyId,
                Name = "Ouvrier",
                CreatedBy = userId,
            }
        );
    }

    private async Task SeedWorkingCalendarAsync(int companyId, int userId, CancellationToken ct)
    {
        if (await _db.WorkingCalendars.AnyAsync(w => w.CompanyId == companyId, ct))
            return;
        for (int day = 0; day <= 6; day++)
        {
            bool isWorking = day >= 1 && day <= 5; // Lun-Ven
            _db.WorkingCalendars.Add(
                new WorkingCalendar
                {
                    CompanyId = companyId,
                    DayOfWeek = day,
                    IsWorkingDay = isWorking,
                    StartTime = isWorking ? TimeSpan.FromHours(8) : null,
                    EndTime = isWorking ? TimeSpan.FromHours(17) : null,
                    CreatedBy = userId,
                }
            );
        }
    }

    private async Task SeedLeaveTypesAsync(int companyId, int userId, CancellationToken ct)
    {
        if (await _db.LeaveTypes.AnyAsync(l => l.CompanyId == companyId, ct))
            return;

        var annualLeave = new LeaveType
        {
            CompanyId = companyId,
            LeaveCode = "ANNUAL",
            LeaveNameFr = "Congé annuel",
            LeaveNameAr = "إجازة سنوية",
            LeaveNameEn = "Annual Leave",
            LeaveDescription = "Congé annuel légal",
            IsActive = true,
            CreatedBy = userId,
        };
        _db.LeaveTypes.Add(annualLeave);
        await _db.SaveChangesAsync(ct); // flush pour avoir l'ID

        _db.LeaveTypePolicies.Add(
            new LeaveTypePolicy
            {
                CompanyId = companyId,
                LeaveTypeId = annualLeave.Id,
                IsEnabled = true,
                AccrualMethod = LeaveAccrualMethod.Monthly,
                DaysPerMonthAdult = 1.5m,
                DaysPerMonthMinor = 2.0m,
                BonusDaysPerYearAfter5Years = 1.5m,
                RequiresEligibility6Months = true,
                RequiresBalance = true,
                AnnualCapDays = 30,
                AllowCarryover = true,
                MaxCarryoverYears = 1,
                UseWorkingCalendar = true,
                CreatedBy = userId,
            }
        );

        // Congés exceptionnels légaux
        var exceptional = new[]
        {
            ("MARIAGE", "Mariage de l'employé", "زواج الموظف", "Employee Marriage", 4),
            ("NAISSANCE", "Naissance / adoption", "ولادة / تبني", "Birth / Adoption", 3),
            ("DECES", "Décès conjoint/enfant", "وفاة الزوج/الطفل", "Death of spouse/child", 3),
            ("MALADIE", "Congé maladie", "إجازة مرضية", "Sick Leave", 0),
        };
        foreach (var (code, nameFr, nameAr, nameEn, days) in exceptional)
        {
            _db.LeaveTypes.Add(
                new LeaveType
                {
                    CompanyId = companyId,
                    LeaveCode = code,
                    LeaveNameFr = nameFr,
                    LeaveNameAr = nameAr,
                    LeaveNameEn = nameEn,
                    LeaveDescription = nameFr,
                    IsActive = true,
                    CreatedBy = userId,
                }
            );
        }
    }
}
