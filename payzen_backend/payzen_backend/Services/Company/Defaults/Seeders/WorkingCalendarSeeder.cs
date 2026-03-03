using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Company;
using payzen_backend.Services.Company.Defaults.Catalog;

namespace payzen_backend.Services.Company.Defaults.Seeders
{
    /// <summary>
    /// Crée le calendrier de travail par défaut (7 jours) pour une nouvelle company.
    /// </summary>
    public class WorkingCalendarSeeder
    {
        private readonly AppDbContext _db;

        public WorkingCalendarSeeder(AppDbContext db)
        {
            _db = db;
        }

        public async Task SeedAsync(int companyId, int userId)
        {
            var days = DefaultWorkingCalendar.GetDefaults();
            foreach (var day in days)
            {
                var exists = await _db.WorkingCalendars
                    .AnyAsync(wc => wc.CompanyId == companyId && wc.DayOfWeek == day.DayOfWeek && wc.DeletedAt == null);
                if (exists) continue;

                _db.WorkingCalendars.Add(new WorkingCalendar
                {
                    CompanyId = companyId,
                    DayOfWeek = day.DayOfWeek,
                    IsWorkingDay = day.IsWorkingDay,
                    StartTime = day.StartTime,
                    EndTime = day.EndTime,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                });
            }
            await _db.SaveChangesAsync();
        }
    }
}
