using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Leave;

namespace payzen_backend.Services
{
    public class WorkingDaysCalculator
    {
        private readonly AppDbContext _db;

        public WorkingDaysCalculator(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Calcule les jours ouvrables � d�duire en excluant week-ends et jours f�ri�s.
        /// </summary>
        public async Task<decimal> CalculateWorkingDaysAsync(int companyId, DateOnly startDate, DateOnly endDate)
        {
            // Charger les jours f�ri�s de l'entreprise (et �ventuellement nationaux) dans l'intervalle
            var holidays = await _db.Holidays
                .AsNoTracking()
                .Where(h => h.DeletedAt == null
                            && (h.CompanyId == companyId || h.CompanyId == null) // company ou global
                            && h.HolidayDate >= startDate
                            && h.HolidayDate <= endDate
                            && h.IsMandatory) // n�inclure que les f�ri�s impactant l�absence
                .Select(h => h.HolidayDate)
                .ToListAsync();

            var holidaySet = holidays.ToHashSet();

            decimal workingDays = 0;
            var current = startDate;

            while (current <= endDate)
            {
                var dayOfWeek = current.DayOfWeek;
                var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
                var isHoliday = holidaySet.Contains(current);

                if (!isWeekend && !isHoliday)
                {
                    workingDays++;
                }

                current = current.AddDays(1);
            }

            return workingDays;
        }
    }
}