using Microsoft.EntityFrameworkCore;
using Payzen.Application.Interfaces;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services;

/// <summary>
/// Calcul des jours ouvrables selon le calendrier de l'entreprise et les jours fériés.
/// Règle Maroc : vendredi compte pour 1.5 jours (vendredi + samedi inclus dans le congé).
/// </summary>
public class WorkingDaysCalculatorService : IWorkingDaysCalculator
{
    private readonly AppDbContext _db;
    public WorkingDaysCalculatorService(AppDbContext db) => _db = db;

    public async Task<decimal> CalculateWorkingDaysAsync(
        int companyId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        var holidays = await _db.Holidays
            .AsNoTracking()
            .Where(h => h.DeletedAt == null
                     && (h.CompanyId == companyId || h.CompanyId == null)
                     && h.HolidayDate >= startDate
                     && h.HolidayDate <= endDate
                     && h.IsMandatory)
            .Select(h => h.HolidayDate)
            .ToListAsync(ct);

        var holidaySet = holidays.ToHashSet();
        decimal workingDays = 0;
        var current = startDate;
        int i = 1;
        while (current <= endDate)
        {
            var dow = current.DayOfWeek;
            var isWeekend = dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday;
            var isHoliday = holidaySet.Contains(current);

            if (!isWeekend && !isHoliday)
            {
                workingDays++;
                // Vendredi → samedi inclus dans le congé marocain
                if (dow == DayOfWeek.Friday)
                    workingDays++;
            }
            current = current.AddDays(1);
        }
        return workingDays;
    }
}
