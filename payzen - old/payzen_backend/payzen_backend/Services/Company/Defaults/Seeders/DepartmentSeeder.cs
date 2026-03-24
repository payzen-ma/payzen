using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Company;
using payzen_backend.Services.Company.Defaults.Catalog;

namespace payzen_backend.Services.Company.Defaults.Seeders
{
    /// <summary>
    /// Crée les départements par défaut pour une nouvelle company.
    /// </summary>
    public class DepartmentSeeder
    {
        private readonly AppDbContext _db;

        public DepartmentSeeder(AppDbContext db)
        {
            _db = db;
        }

        public async Task SeedAsync(int companyId, int userId)
        {
            var names = DefaultDepartments.GetDefaults();
            foreach (var name in names)
            {
                var exists = await _db.Departement
                    .AnyAsync(d => d.CompanyId == companyId && d.DepartementName == name && d.DeletedAt == null);
                if (exists) continue;

                _db.Departement.Add(new Departement
                {
                    DepartementName = name,
                    CompanyId = companyId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                });
            }
            await _db.SaveChangesAsync();
        }
    }
}
