using Microsoft.EntityFrameworkCore;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Company;
using Payzen.Domain.Entities.Employee;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Company;

/// <summary>
/// Initialise les données par défaut d'une nouvelle entreprise après création :
///   - Calendrier de travail (Lun-Ven)
///   - Types de contrat légaux par défaut (CDI, CDD, Intérim, Stage)
///   - Départements, postes (intitulés de fonction), catégories d'employés
/// Idempotent : ne recrée pas ce qui existe déjà.
/// </summary>
public class CompanyOnboardingService : ICompanyOnboardingService
{
    private readonly AppDbContext _db;

    public CompanyOnboardingService(AppDbContext db) => _db = db;

    public async Task OnboardAsync(int companyId, int userId, CancellationToken ct = default)
    {
        await SeedWorkingCalendarAsync(companyId, userId, ct);
        await SeedDefaultContractTypesAsync(companyId, userId, ct);
        await SeedDepartmentsAsync(companyId, userId, ct);
        await SeedJobPositionsAsync(companyId, userId, ct);
        await SeedEmployeeCategoriesAsync(companyId, userId, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedWorkingCalendarAsync(int companyId, int userId, CancellationToken ct)
    {
        var exists = await _db.WorkingCalendars.AnyAsync(w => w.CompanyId == companyId, ct);
        if (exists) return;

        // Lundi (1) → Vendredi (5) = jours travaillés, Samedi (6) + Dimanche (0) = repos
        for (int day = 0; day <= 6; day++)
        {
            bool isWorkingDay = day >= 1 && day <= 5;
            _db.WorkingCalendars.Add(new WorkingCalendar
            {
                CompanyId    = companyId,
                DayOfWeek    = day,
                IsWorkingDay = isWorkingDay,
                StartTime    = isWorkingDay ? new TimeSpan(8, 30, 0) : null,
                EndTime      = isWorkingDay ? new TimeSpan(17, 30, 0) : null,
                CreatedBy    = userId
            });
        }
    }

    private async Task SeedDefaultContractTypesAsync(int companyId, int userId, CancellationToken ct)
    {
        var exists = await _db.ContractTypes.AnyAsync(c => c.CompanyId == companyId, ct);
        if (exists) return;

        var defaults = new[]
        {
            "CDI",
            "CDD",
            "Contrat d'intérim",
            "Contrat de stage"
        };

        foreach (var name in defaults)
        {
            _db.ContractTypes.Add(new ContractType
            {
                ContractTypeName = name,
                CompanyId        = companyId,
                CreatedBy        = userId
            });
        }
    }

    private async Task SeedDepartmentsAsync(int companyId, int userId, CancellationToken ct)
    {
        if (await _db.Departements.AnyAsync(d => d.CompanyId == companyId, ct)) return;

        var defaults = new[]
        {
            "Direction",
            "Ressources Humaines",
            "Comptabilité",
            "Commercial",
            "Informatique",
            "Production"
        };

        foreach (var name in defaults)
        {
            _db.Departements.Add(new Departement
            {
                CompanyId       = companyId,
                DepartementName = name,
                CreatedBy       = userId
            });
        }
    }

    private async Task SeedJobPositionsAsync(int companyId, int userId, CancellationToken ct)
    {
        if (await _db.JobPositions.AnyAsync(j => j.CompanyId == companyId, ct)) return;

        var defaults = new[]
        {
            "Directeur Général",
            "Responsable RH",
            "Comptable",
            "Commercial",
            "Développeur",
            "Technicien"
        };

        foreach (var name in defaults)
        {
            _db.JobPositions.Add(new JobPosition
            {
                CompanyId = companyId,
                Name      = name,
                CreatedBy = userId
            });
        }
    }

    private async Task SeedEmployeeCategoriesAsync(int companyId, int userId, CancellationToken ct)
    {
        if (await _db.EmployeeCategories.AnyAsync(c => c.CompanyId == companyId, ct)) return;

        _db.EmployeeCategories.Add(new EmployeeCategory
        {
            CompanyId = companyId,
            Name      = "Cadre",
            Mode      = EmployeeCategoryMode.Attendance,
            CreatedBy = userId
        });
        _db.EmployeeCategories.Add(new EmployeeCategory
        {
            CompanyId = companyId,
            Name      = "Ouvrier",
            Mode      = EmployeeCategoryMode.Attendance,
            CreatedBy = userId
        });
    }
}
