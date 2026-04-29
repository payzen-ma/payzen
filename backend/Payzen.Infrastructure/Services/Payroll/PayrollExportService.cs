using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Employee;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Payroll;

/// <summary>
/// Génère les états réglementaires marocains à partir des PayrollResult calculés.
///   - Journal de Paie mensuel
///   - État CNSS (format Damancom + PDF)
///   - État IR (retenue à la source + PDF)
/// </summary>
public class PayrollExportService : IPayrollExportService
{
    private readonly AppDbContext _db;
    private readonly IPayrollWorkingDaysService _payrollWorkingDaysService;

    public PayrollExportService(AppDbContext db, IPayrollWorkingDaysService payrollWorkingDaysService)
    {
        _db = db;
        _payrollWorkingDaysService = payrollWorkingDaysService;
    }

    // ── Journal de Paie ───────────────────────────────────────────────────────

    public async Task<ServiceResult<List<JournalPaieRow>>> GetJournalPaieAsync(
        int companyId,
        int year,
        int month,
        int? monthTo = null,
        CancellationToken ct = default
    )
    {
        var monthEnd = monthTo ?? month;
        if (month is < 1 or > 12 || monthEnd is < 1 or > 12 || monthEnd < month)
            return ServiceResult<List<JournalPaieRow>>.Fail(
                "Période invalide: month et monthTo doivent être entre 1 et 12."
            );

        var results = await LoadResultsWithEmployee(companyId, year, month, monthEnd, ct);
        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = new DateOnly(year, monthEnd, DateTime.DaysInMonth(year, monthEnd));

        var rows = new List<JournalPaieRow>(results.Count);
        foreach (var pr in results)
        {
            var contract = ResolveContractForPeriod(pr.Employee, year, monthEnd);
            var hireDate =
                contract?.StartDate.Date
                ?? pr.Employee.Contracts.OrderBy(c => c.StartDate).Select(c => (DateTime?)c.StartDate).FirstOrDefault();
            var workingDays = await _payrollWorkingDaysService.CalculateWorkingDaysAsync(
                pr.EmployeeId,
                periodStart,
                periodEnd,
                ct
            );

            rows.Add(
                new JournalPaieRow
                {
                    Matricule = pr.Employee.Matricule?.ToString() ?? pr.EmployeeId.ToString(),
                    Nom = pr.Employee.LastName,
                    Prenom = pr.Employee.FirstName,
                    DateNaissance = pr.Employee.DateOfBirth.ToString("dd/MM/yyyy"),
                    NbrJrsTravailles = workingDays,
                    JrsFeries = pr.JoursFeries ?? 0m,
                    SBPlusConge = (pr.SalaireBase ?? 0m) + (pr.Conges ?? 0m),
                    SalaireBaseDuMois = pr.SalaireBase ?? 0m,
                    NbrDeductions = Math.Clamp(26 - workingDays, 0, 26),
                    CNSSPartSalariale = pr.CnssPartSalariale ?? 0m,
                    AMOPartSalariale = pr.AmoPartSalariale ?? 0m,
                    IRAPayer = pr.ImpotRevenu ?? 0m,
                    InteretLogement = pr.InteretSurLogement ?? 0m,
                    HeuresNormales = workingDays * 8m,
                    HeuresSup50 = pr.HeuresSupp50 ?? 0m,
                    HeuresSup25 = pr.HeuresSupp25 ?? 0m,
                    HeuresSup100 = pr.HeuresSupp100 ?? 0m,
                    CIN = pr.Employee.CinNumber,
                    CNSS = pr.Employee.CnssNumber ?? string.Empty,
                    DateEmbauche = hireDate?.ToString("dd/MM/yyyy") ?? string.Empty,
                    Fonction = contract?.JobPosition?.Name ?? string.Empty,
                    NbrJrsConge = pr.Conges ?? 0m,
                    MTAnciennete = pr.PrimeAnciennete ?? 0m,
                    LesPrimesImposables = pr.TotalPrimesImposables ?? 0m,
                    BrutImposable = pr.BrutImposable ?? 0m,
                    MutuellePartSalariale = pr.MutuellePartSalariale ?? 0m,
                    CIMR = pr.CimrPartSalariale ?? 0m,
                    NetImposable = pr.NetImposable ?? 0m,
                    BrutGlobal = pr.TotalBrut ?? 0m,
                    Avance = pr.AvanceSurSalaire ?? 0m,
                }
            );
        }

        rows = rows.OrderBy(r => r.Nom).ThenBy(r => r.Prenom).ToList();

        return ServiceResult<List<JournalPaieRow>>.Ok(rows);
    }

    // ── État CNSS ────────────────────────────────────────────────────────────

    public async Task<ServiceResult<List<EtatCnssRow>>> GetEtatCnssAsync(
        int companyId,
        int year,
        int month,
        CancellationToken ct = default
    )
    {
        var results = await LoadResultsWithEmployee(companyId, year, month, month, ct);

        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var rows = new List<EtatCnssRow>(results.Count);
        foreach (var pr in results)
        {
            var nombreJoursDeclare = await _payrollWorkingDaysService.CalculateWorkingDaysAsync(
                pr.EmployeeId,
                periodStart,
                periodEnd,
                ct
            );

            rows.Add(
                new EtatCnssRow
                {
                    NomPrenom = $"{pr.Employee.LastName} {pr.Employee.FirstName}",
                    NumeroCnss = pr.Employee.CnssNumber ?? string.Empty,
                    SalaireBrutDeclare = pr.CnssBase ?? pr.TotalBrut ?? 0m,
                    NombreJoursDeclare = nombreJoursDeclare,
                }
            );
        }

        rows = rows.OrderBy(r => r.NomPrenom).ToList();

        return ServiceResult<List<EtatCnssRow>>.Ok(rows);
    }

    public async Task<ServiceResult<EtatCnssPdfData>> GetEtatCnssPdfDataAsync(
        int companyId,
        int year,
        int month,
        CancellationToken ct = default
    )
    {
        var company = await _db.Companies.FindAsync(new object[] { companyId }, ct);
        if (company == null)
            return ServiceResult<EtatCnssPdfData>.Fail("Société introuvable.");

        var results = await LoadResultsWithEmployee(companyId, year, month, month, ct);

        // Taux CNSS Décret 2.25.266 (2025)
        const decimal CNSS_RG_SAL = 0.0448m;
        const decimal CNSS_AMO_SAL = 0.0226m;
        const decimal CNSS_RG_PAT = 0.0898m;
        const decimal CNSS_AF_PAT = 0.0640m;
        const decimal CNSS_FP_PAT = 0.0160m;
        const decimal CNSS_AMO_PAT = 0.0226m;
        const decimal CNSS_AMO_PAT2 = 0.0185m;

        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var rows = new List<EtatCnssFullRow>(results.Count);
        for (var i = 0; i < results.Count; i++)
        {
            var pr = results[i];
            var baseCnss = pr.CnssBase ?? Math.Min(pr.TotalBrut ?? 0m, 6000m);
            var brut = pr.TotalBrut ?? 0m;
            var rgSal = Math.Round(baseCnss * CNSS_RG_SAL, 2);
            var amoSal = Math.Round(brut * CNSS_AMO_SAL, 2);
            var rgPat = Math.Round(baseCnss * CNSS_RG_PAT, 2);
            var afPat = Math.Round(brut * CNSS_AF_PAT, 2);
            var fpPat = Math.Round(brut * CNSS_FP_PAT, 2);
            var amoPat = Math.Round(brut * CNSS_AMO_PAT, 2);
            var amoPat2 = Math.Round(brut * CNSS_AMO_PAT2, 2);
            var nombreJours = await _payrollWorkingDaysService.CalculateWorkingDaysAsync(
                pr.EmployeeId,
                periodStart,
                periodEnd,
                ct
            );

            rows.Add(
                new EtatCnssFullRow
                {
                    Ordre = i + 1,
                    NomPrenom = $"{pr.Employee.LastName} {pr.Employee.FirstName}",
                    NumeroCnss = pr.Employee.CnssNumber ?? string.Empty,
                    CIN = pr.Employee.CinNumber,
                    NombreJours = nombreJours,
                    SalaireBrut = brut,
                    BaseCnss = baseCnss,
                    RgSalarial = rgSal,
                    AmoSalarial = amoSal,
                    RgPatronal = rgPat,
                    AfPatronal = afPat,
                    FpPatronal = fpPat,
                    AmoPatronal = amoPat,
                    CotisationAmo = Math.Round((amoSal + amoPat), 2),
                    ParticipationAmo = amoPat2,
                }
            );
        }

        rows = rows.OrderBy(r => r.NomPrenom).ToList();

        return ServiceResult<EtatCnssPdfData>.Ok(
            new EtatCnssPdfData
            {
                CompanyName = company.CompanyName,
                CompanyCnss = company.CnssNumber,
                CompanyAddress = company.CompanyAddress,
                CompanyIce = company.IceNumber ?? string.Empty,
                Month = month,
                Year = year,
                Rows = rows,
            }
        );
    }

    // ── État IR ──────────────────────────────────────────────────────────────

    public async Task<ServiceResult<List<EtatIrRow>>> GetEtatIrAsync(
        int companyId,
        int year,
        int month,
        CancellationToken ct = default
    )
    {
        var results = await LoadResultsWithEmployee(companyId, year, month, month, ct);

        var rows = results
            .Select(pr => new EtatIrRow
            {
                NomPrenom = $"{pr.Employee.LastName} {pr.Employee.FirstName}",
                CIN = pr.Employee.CinNumber,
                CNSS = pr.Employee.CnssNumber ?? string.Empty,
                BrutImposable = pr.BrutImposable ?? 0m,
                IRRetenu = pr.ImpotRevenu ?? 0m,
            })
            .OrderBy(r => r.NomPrenom)
            .ToList();

        return ServiceResult<List<EtatIrRow>>.Ok(rows);
    }

    public async Task<ServiceResult<EtatIrPdfData>> GetEtatIrPdfDataAsync(
        int companyId,
        int year,
        int month,
        CancellationToken ct = default
    )
    {
        var company = await _db.Companies.FindAsync(new object[] { companyId }, ct);
        if (company == null)
            return ServiceResult<EtatIrPdfData>.Fail("Société introuvable.");

        var results = await LoadResultsWithEmployee(companyId, year, month, month, ct);

        var rows = results
            .Select(pr => new EtatIrFullRow
            {
                Matricule = pr.Employee.Matricule ?? pr.EmployeeId,
                NomPrenom = $"{pr.Employee.LastName} {pr.Employee.FirstName}",
                SalImposable = pr.BrutImposable ?? 0m,
                MontantIGR = pr.ImpotRevenu ?? 0m,
            })
            .OrderBy(r => r.NomPrenom)
            .ToList();

        return ServiceResult<EtatIrPdfData>.Ok(
            new EtatIrPdfData
            {
                CompanyName = company.CompanyName,
                CompanyAddress = company.CompanyAddress,
                Month = month,
                Year = year,
                Rows = rows,
            }
        );
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<List<Domain.Entities.Payroll.PayrollResult>> LoadResultsWithEmployee(
        int companyId,
        int year,
        int monthFrom,
        int monthTo,
        CancellationToken ct
    ) =>
        await _db
            .PayrollResults.Where(pr =>
                pr.CompanyId == companyId
                && pr.Year == year
                && pr.Month >= monthFrom
                && pr.Month <= monthTo
                && pr.Status == Domain.Enums.PayrollResultStatus.OK
                && pr.DeletedAt == null
            )
            .Include(pr => pr.Employee)
                .ThenInclude(e => e.Contracts)
                    .ThenInclude(c => c.JobPosition)
            .OrderBy(pr => pr.Employee.LastName)
            .ThenBy(pr => pr.Employee.FirstName)
            .ThenBy(pr => pr.Month)
            .ToListAsync(ct);

    private static EmployeeContract? ResolveContractForPeriod(
        Payzen.Domain.Entities.Employee.Employee employee,
        int year,
        int month
    )
    {
        if (employee.Contracts == null || employee.Contracts.Count == 0)
            return null;

        var periodEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        return employee
            .Contracts.Where(c => c.StartDate <= periodEnd && (c.EndDate == null || c.EndDate.Value.Date >= periodEnd))
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefault();
    }
}
