using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Company;
using payzen_backend.Services.Company.Defaults.Catalog;

namespace payzen_backend.Services.Company.Defaults.Seeders
{
    /// <summary>
    /// Crée les types de contrat par défaut pour une nouvelle company (CDI, CDD, Stage, etc.).
    /// </summary>
    public class ContractTypeSeeder
    {
        private readonly AppDbContext _db;

        public ContractTypeSeeder(AppDbContext db)
        {
            _db = db;
        }

        public async Task SeedAsync(int companyId, int userId)
        {
            var defaults = DefaultContractTypes.GetDefaults();
            var legalTypes = await _db.LegalContractTypes
                .AsNoTracking()
                .Where(l => l.DeletedAt == null)
                .ToDictionaryAsync(l => l.Code.ToUpperInvariant(), l => l.Id);
            var statePrograms = await _db.StateEmploymentPrograms
                .AsNoTracking()
                .Where(s => s.DeletedAt == null)
                .ToDictionaryAsync(s => s.Code.ToUpperInvariant(), s => s.Id);

            foreach (var def in defaults)
            {
                var exists = await _db.ContractTypes
                    .AnyAsync(ct => ct.CompanyId == companyId && ct.ContractTypeName == def.Name && ct.DeletedAt == null);
                if (exists) continue;

                int? legalId = null;
                if (!string.IsNullOrEmpty(def.LegalContractTypeCode) && legalTypes.TryGetValue(def.LegalContractTypeCode.ToUpperInvariant(), out var lid))
                    legalId = lid;

                int? stateId = null;
                if (!string.IsNullOrEmpty(def.StateProgramCode) && statePrograms.TryGetValue(def.StateProgramCode.ToUpperInvariant(), out var sid))
                    stateId = sid;

                _db.ContractTypes.Add(new ContractType
                {
                    ContractTypeName = def.Name,
                    CompanyId = companyId,
                    LegalContractTypeId = legalId,
                    StateEmploymentProgramId = stateId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                });
            }
            await _db.SaveChangesAsync();
        }
    }
}
