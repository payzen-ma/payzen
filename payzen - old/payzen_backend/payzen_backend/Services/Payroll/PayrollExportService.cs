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
                    .ThenInclude(e => e.Contracts.Where(c => c.EndDate == null || c.EndDate > DateTime.Now).OrderByDescending(c => c.StartDate).Take(1))
                        .ThenInclude(c => c.JobPosition)
                .Include(r => r.Primes)
                .Where(r => r.CompanyId == companyId
                         && r.Year  == year
                         && r.Month == month
                         && r.Status == PayrollResultStatus.OK)
                .OrderBy(r => r.Employee.Matricule)
                .ThenBy(r => r.Employee.LastName)
                .ToListAsync();

            return results.Select(r =>
            {
                var activeContract = r.Employee.Contracts?.FirstOrDefault();
                var situationFamiliale = r.Employee.MaritalStatusId switch
                {
                    1 => "Célibataire",
                    2 => "Marié(e)",
                    3 => "Divorcé(e)",
                    4 => "Veuf(ve)",
                    _ => string.Empty
                };

                // Nombre de jours travaillés (26 par défaut, moins les congés)
                var nbrJrsTravailles = 26 - (int)(r.Conges ?? 0m);
                var nbrDeductions = 0;
                if (r.CnssPartSalariale > 0) nbrDeductions++;
                if (r.AmoPartSalariale > 0) nbrDeductions++;
                if (r.CimrPartSalariale > 0) nbrDeductions++;
                if (r.MutuellePartSalariale > 0) nbrDeductions++;
                if (r.ImpotRevenu > 0) nbrDeductions++;

                return new JournalPaieRow
                {
                    // Ligne 1
                    Matricule = r.Employee.Matricule?.ToString() ?? string.Empty,
                    Nom = r.Employee.LastName.ToUpperInvariant(),
                    Prenom = r.Employee.FirstName,
                    DateNaissance = r.Employee.DateOfBirth.ToString("dd/MM/yyyy"),
                    NbrJrsTravailles = nbrJrsTravailles,
                    JrsFeries = r.JoursFeries ?? 0m,
                    SBPlusConge = (r.SalaireBase ?? 0m) + (r.Conges ?? 0m),
                    SalaireBaseDuMois = r.SalaireBase ?? 0m,
                    NbrDeductions = nbrDeductions,
                    CNSSPartSalariale = r.CnssPartSalariale ?? 0m,
                    AMOPartSalariale = r.AmoPartSalariale ?? 0m,
                    FraisProfesADeduireM = r.FraisProfessionnels ?? 0m,
                    IRAPayer = r.ImpotRevenu ?? 0m,
                    SalaireNet = r.NetAPayer ?? 0m,
                    InteretLogement = r.InteretSurLogement ?? 0m,
                    HeuresNormales = 191m, // 26 jours × 8h × (5/6) ≈ 173h ou standard 191h
                    HeuresSup50 = r.HeuresSupp50 ?? 0m,
                    
                    // Ligne 2
                    SF = situationFamiliale,
                    CIN = r.Employee.CinNumber ?? string.Empty,
                    CNSS = r.Employee.CnssNumber ?? string.Empty,
                    DateEmbauche = activeContract?.StartDate.ToString("dd/MM/yyyy") ?? string.Empty,
                    Fonction = activeContract?.JobPosition?.Name ?? string.Empty,
                    NbrJrsConge = r.Conges ?? 0m,
                    MTAnciennete = r.PrimeAnciennete ?? 0m,
                    LesPrimesImposables = r.TotalPrimesImposables ?? 0m,
                    BrutImposable = r.BrutImposable ?? 0m,
                    MutuellePartSalariale = r.MutuellePartSalariale ?? 0m,
                    CIMR = r.CimrPartSalariale ?? 0m,
                    NetImposable = r.NetImposable ?? 0m,
                    MtExonere = r.TotalIndemnites ?? 0m,
                    BrutGlobal = r.TotalBrut ?? 0m,
                    Avance = r.AvanceSurSalaire ?? 0m,
                    HeuresSup25 = r.HeuresSupp25 ?? 0m,
                    HeuresSup100 = r.HeuresSupp100 ?? 0m
                };
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
        // État CNSS — données complètes pour PDF
        // ─────────────────────────────────────────────────────────────────────

        public async Task<EtatCnssPdfData> GetEtatCnssPdfData(int companyId, int year, int month)
        {
            _logger.LogInformation("GetEtatCnssPdfData company={CompanyId} {Month}/{Year}", companyId, month, year);

            var company = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId)
                ?? throw new Exception($"Entreprise {companyId} introuvable.");

            var results = await _db.PayrollResults
                .AsNoTracking()
                .Include(r => r.Employee)
                .Where(r => r.CompanyId == companyId
                         && r.Year  == year
                         && r.Month == month
                         && r.Status == PayrollResultStatus.OK
                         && r.Employee.CnssNumber != null)
                .OrderBy(r => r.Employee.LastName)
                .ThenBy(r => r.Employee.FirstName)
                .ToListAsync();

            var rows = results.Select((r, idx) =>
            {
                var baseCnss = r.CnssBase ?? Math.Min(r.TotalBrut ?? 0m, 6_000m);
                var brut     = r.TotalBrut ?? r.BrutImposable ?? 0m;

                var rgSal         = r.CnssPartSalariale ?? Math.Round(baseCnss * 0.0448m, 2);
                var amoSal        = r.AmoPartSalariale  ?? Math.Round(brut     * 0.0226m, 2);
                var rgPat         = Math.Round(baseCnss * 0.0898m, 2);
                var afPat         = Math.Round(brut     * 0.0640m, 2);
                var fpPat         = Math.Round(brut     * 0.0160m, 2);
                // AMO : cotisation = sal(2,26%) + pat base(2,26%) = 4,52% ; participation pat = 1,85%
                var cotisationAmo  = Math.Round(brut * 0.0452m, 2);
                var participAmo    = Math.Round(brut * 0.0185m, 2);
                var amoPat         = r.AmoPartPatronale ?? (cotisationAmo - amoSal + participAmo);

                return new EtatCnssFullRow
                {
                    Ordre           = idx + 1,
                    NomPrenom       = $"{r.Employee.LastName} {r.Employee.FirstName}".Trim().ToUpperInvariant(),
                    NumeroCnss      = r.Employee.CnssNumber!,
                    CIN             = r.Employee.CinNumber ?? string.Empty,
                    NombreJours     = 26,
                    SalaireBrut     = brut,
                    BaseCnss        = baseCnss,
                    RgSalarial      = rgSal,
                    AmoSalarial     = amoSal,
                    RgPatronal      = rgPat,
                    AfPatronal      = afPat,
                    FpPatronal      = fpPat,
                    AmoPatronal     = amoPat,
                    CotisationAmo   = cotisationAmo,
                    ParticipationAmo = participAmo,
                };
            }).ToList();

            return new EtatCnssPdfData
            {
                CompanyName    = company.CompanyName,
                CompanyCnss    = company.CnssNumber,
                CompanyAddress = company.CompanyAddress,
                CompanyIce     = company.IceNumber ?? string.Empty,
                Month          = month,
                Year           = year,
                Rows           = rows,
            };
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

        /// <summary>
        /// État IR enrichi pour export PDF.
        /// </summary>
        public async Task<EtatIrPdfData> GetEtatIrPdfData(int companyId, int year, int month)
        {
            _logger.LogInformation("GetEtatIrPdfData company={CompanyId} {Month}/{Year}", companyId, month, year);

            var company = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
                throw new Exception($"Entreprise {companyId} introuvable.");

            var results = await _db.PayrollResults
                .AsNoTracking()
                .Include(r => r.Employee)
                .Where(r => r.CompanyId == companyId
                         && r.Year  == year
                         && r.Month == month
                         && r.Status == PayrollResultStatus.OK)
                .OrderBy(r => r.Employee.Matricule)
                .ThenBy(r => r.Employee.LastName)
                .ThenBy(r => r.Employee.FirstName)
                .ToListAsync();

            var rows = results.Select(r => new EtatIrFullRow
            {
                Matricule     = r.Employee.Matricule ?? 0,
                NomPrenom     = $"{r.Employee.LastName} {r.Employee.FirstName}".Trim().ToUpperInvariant(),
                SalImposable  = r.BrutImposable ?? r.TotalBrut ?? 0m,
                MontantIGR    = r.ImpotRevenu ?? 0m
            }).ToList();

            return new EtatIrPdfData
            {
                CompanyName    = company.CompanyName,
                CompanyAddress = company.CompanyAddress,
                Month          = month,
                Year           = year,
                Rows           = rows
            };
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
