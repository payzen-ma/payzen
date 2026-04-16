using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Payzen.Domain.Entities.Company;
using Payzen.Infrastructure.Persistence;
using Payzen.Infrastructure.Services;

namespace Payzen.Tests.Unit;

/// <summary>
/// Tests du calculateur de jours ouvrables — règle marocaine vendredi inclus.
/// Utilise EF InMemory (pas de SQL Server nécessaire).
/// </summary>
public class WorkingDaysTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task SemainePleine_Lun_Ven_5Jours()
    {
        // Lundi 10 mars au vendredi 14 mars 2025 = 5 jours
        // Mais vendredi ajoute 1 extra selon règle marocaine → 6 jours
        using var db = CreateDb();
        var svc = new WorkingDaysCalculatorService(db);
        var start = new DateOnly(2025, 3, 10); // Lundi
        var end = new DateOnly(2025, 3, 14); // Vendredi

        var jours = await svc.CalculateWorkingDaysAsync(99, start, end);

        // Lun+Mar+Mer+Jeu = 4, Vendredi = 1 + 1 (extra samedi) = 6
        jours.Should().Be(6m);
    }

    [Fact]
    public async Task WeekEnd_SamediDimanche_ZeroJours()
    {
        using var db = CreateDb();
        var svc = new WorkingDaysCalculatorService(db);
        var start = new DateOnly(2025, 3, 15); // Samedi
        var end = new DateOnly(2025, 3, 16); // Dimanche

        var jours = await svc.CalculateWorkingDaysAsync(99, start, end);

        jours.Should().Be(0m);
    }

    [Fact]
    public async Task JourFerie_ExcluDuCompte()
    {
        // Un jour férié dans la semaine doit réduire le décompte
        using var db = CreateDb();
        db.Holidays.Add(
            new Holiday
            {
                CompanyId = 1,
                HolidayDate = new DateOnly(2025, 3, 12), // Mercredi
                NameFr = "Fête test",
                NameAr = "HolidayAR",
                NameEn = "Test Holiday",
                IsMandatory = true,
                CreatedBy = 1,
            }
        );
        await db.SaveChangesAsync();

        var svc = new WorkingDaysCalculatorService(db);
        var start = new DateOnly(2025, 3, 10); // Lundi
        var end = new DateOnly(2025, 3, 14); // Vendredi

        var avecFerie = await svc.CalculateWorkingDaysAsync(1, start, end);
        var sansFerie = await new WorkingDaysCalculatorService(CreateDb()).CalculateWorkingDaysAsync(99, start, end);

        avecFerie.Should().BeLessThan(sansFerie);
    }
}
