using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Employee;
using payzen_backend.Services.Company.Defaults.Catalog;

namespace payzen_backend.Services.Company.Defaults.Seeders
{
    /// <summary>
    /// Crée les catégories d'employés par défaut pour une nouvelle company.
    /// </summary>
    public class EmployeeCategorySeeder
    {
        private readonly AppDbContext _db;

        public EmployeeCategorySeeder(AppDbContext db)
        {
            _db = db;
        }

        public async Task SeedAsync(int companyId, int userId)
        {
            var defaults = DefaultCategories.GetDefaults();
            foreach (var def in defaults)
            {
                var exists = await _db.EmployeeCategories
                    .AnyAsync(c => c.CompanyId == companyId && c.Name == def.Name && c.DeletedAt == null);
                if (exists) continue;

                _db.EmployeeCategories.Add(new EmployeeCategory
                {
                    CompanyId = companyId,
                    Name = def.Name,
                    Mode = def.Mode,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                });
            }
            await _db.SaveChangesAsync();
        }
    }
}
