using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.DTOs.Payroll;
using payzen_backend.Models.Payroll;

namespace payzen_backend.Services.Payroll
{
    /// <summary>
    /// Génère les exports de paie marocains :
    ///   - Journal de Paie
    ///   - État CNSS (format Damancom)
    ///   - État IR
    /// Seuls les bulletins au statut OK (calculés et validés) sont inclus.
    /// Le filtre soft-delete global d'AppDbContext est automatiquement appliqué.
    /// </summary>
    public class PayrollExportService : IPayrollExportService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PayrollExportService> _logger;

        public PayrollExportService(AppDbContext db, ILogger<PayrollExportService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Journal de Paie
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<JournalPaieRow>> GetJournalPaie(int companyId, int year, int month)
        {
            _logger.LogInformation("GetJournalPaie company={CompanyId} {Month}/{Year}", companyId, month, year);

            var results = await _db.PayrollResults
                .AsNoTracking()
                .Include(r => r.Employee)
                .Include(r => r.Primes)
                .Where(r => r.CompanyId == companyId
                         && r.Year  == year
                         && r.Month == month
                         && r.Status == PayrollResultStatus.OK)
                .OrderBy(r => r.Employee.LastName)
                .ThenBy(r => r.Employee.FirstName)
                .ToListAsync();

            return results.Select(r => new JournalPaieRow
            {
                Matricule          = r.EmployeeId.ToString(),
                NomPrenom          = $"{r.Employee.LastName} {r.Employee.FirstName}".Trim(),
                CIN                = r.Employee.CinNumber ?? string.Empty,
                CNSS               = r.Employee.CnssNumber ?? string.Empty,
                SalaireBase        = r.SalaireBase       ?? 0m,
                TotalBrut          = r.TotalBrut         ?? 0m,
                CotisationsSalariales = r.TotalCotisationsSalariales ?? 0m,
                IR                 = r.ImpotRevenu       ?? 0m,
                NetAPayer          = r.NetAPayer         ?? r.TotalNet ?? 0m,
                DetailsPrimes      = BuildDetailsPrimes(r.Primes)
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // État CNSS (Damancom)
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<EtatCnssRow>> GetEtatCnss(int companyId, int year, int month)
        {
            _logger.LogInformation("GetEtatCnss company={CompanyId} {Month}/{Year}", companyId, month, year);

            var results = await _db.PayrollResults
                .AsNoTracking()
                .Include(r => r.Employee)
                .Where(r => r.CompanyId == companyId
                         && r.Year  == year
                         && r.Month == month
                         && r.Status == PayrollResultStatus.OK
                         && r.Employee.CnssNumber != null)
                .OrderBy(r => r.Employee.CnssNumber)
                .ToListAsync();

            return results.Select(r => new EtatCnssRow
            {
                NomPrenom          = $"{r.Employee.LastName} {r.Employee.FirstName}".Trim(),
                NumeroCnss         = r.Employee.CnssNumber!,
                // La base CNSS est le brut plafonné ; on utilise CnssBase si disponible
                SalaireBrutDeclare = r.CnssBase ?? r.TotalBrut ?? 0m,
                NombreJoursDeclare = 26
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // État IR
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<EtatIrRow>> GetEtatIr(int companyId, int year, int month)
        {
            _logger.LogInformation("GetEtatIr company={CompanyId} {Month}/{Year}", companyId, month, year);

            var results = await _db.PayrollResults
                .AsNoTracking()
                .Include(r => r.Employee)
                .Where(r => r.CompanyId == companyId
                         && r.Year  == year
                         && r.Month == month
                         && r.Status == PayrollResultStatus.OK)
                .OrderBy(r => r.Employee.LastName)
                .ThenBy(r => r.Employee.FirstName)
                .ToListAsync();

            return results.Select(r => new EtatIrRow
            {
                NomPrenom      = $"{r.Employee.LastName} {r.Employee.FirstName}".Trim(),
                CIN            = r.Employee.CinNumber  ?? string.Empty,
                CNSS           = r.Employee.CnssNumber ?? string.Empty,
                BrutImposable  = r.BrutImposable ?? r.TotalBrut ?? 0m,
                IRRetenu       = r.ImpotRevenu   ?? 0m
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers privés
        // ─────────────────────────────────────────────────────────────────────

        private static string BuildDetailsPrimes(ICollection<Models.Payroll.PayrollResultPrime> primes)
        {
            if (primes == null || !primes.Any())
                return string.Empty;

            return string.Join(" | ", primes
                .OrderBy(p => p.Ordre)
                .Select(p => $"{p.Label}: {p.Montant:N2}"));
        }
    }
}
