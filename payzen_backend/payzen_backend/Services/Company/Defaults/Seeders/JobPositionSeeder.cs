using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Company;
using payzen_backend.Services.Company.Defaults.Catalog;

namespace payzen_backend.Services.Company.Defaults.Seeders
{
    /// <summary>
    /// Crée les postes (job positions) par défaut pour une nouvelle company.
    /// </summary>
    public class JobPositionSeeder
    {
        private readonly AppDbContext _db;

        public JobPositionSeeder(AppDbContext db)
        {
            _db = db;
        }

        public async Task SeedAsync(int companyId, int userId)
        {
            var names = DefaultJobPositions.GetDefaults();
            foreach (var name in names)
            {
                var exists = await _db.JobPositions
                    .AnyAsync(j => j.CompanyId == companyId && j.Name == name && j.DeletedAt == null);
                if (exists) continue;

                _db.JobPositions.Add(new JobPosition
                {
                    Name = name,
                    CompanyId = companyId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                });
            }
            await _db.SaveChangesAsync();
        }
    }
}
