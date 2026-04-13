using IronPdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Employee;
using Payzen.Domain.Entities.Leave;
using Payzen.Domain.Entities.Payroll;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;
using System.Globalization;
using System.Text;

namespace Payzen.Infrastructure.Services.Documents;

/// <summary>
/// Génération de documents PDF via IronPDF (ChromePdfRenderer).
/// Bulletins de paie, État CNSS (Damancom), État IR, Journal de Paie CSV.
/// </summary>
public class IronPdfDocumentService : IDocumentService
{
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _db;
    private readonly ILeaveBalanceRecalculationService _leaveBalanceRecalc;
    private readonly ILogger<IronPdfDocumentService> _logger;

    public IronPdfDocumentService(
        IWebHostEnvironment env,
        AppDbContext db,
        ILeaveBalanceRecalculationService leaveBalanceRecalc,
        ILogger<IronPdfDocumentService> logger)
    {
        _env = env;
        _db = db;
        _leaveBalanceRecalc = leaveBalanceRecalc;
        _logger = logger;
    }

    // ── Bulletin de paie ─────────────────────────────────────────────────────

    public async Task<ServiceResult<byte[]>> GeneratePayslipByEmployeePeriodAsync(
        int employeeId, int year, int month,
        int? half,
        CancellationToken ct = default)
    {
        var normalizedPayHalf = half is 0 ? null : half;
        var pr = await _db.PayrollResults.FirstOrDefaultAsync(p =>
            p.EmployeeId == employeeId
            && p.Year == year
            && p.Month == month
            && p.PayHalf == normalizedPayHalf
            && p.DeletedAt == null, ct);
        if (pr == null)
            return ServiceResult<byte[]>.Fail("Aucun bulletin pour cet employé et cette période.");
        return await GeneratePayslipInternalAsync(pr.Id, half, ct);
    }

    public Task<ServiceResult<byte[]>> GeneratePayslipAsync(
        int payrollResultId, CancellationToken ct = default)
        => GeneratePayslipInternalAsync(payrollResultId, null, ct);

    private async Task<ServiceResult<byte[]>> GeneratePayslipInternalAsync(
        int payrollResultId, int? half, CancellationToken ct = default)
    {
        var payroll = await _db.PayrollResults
            .Include(pr => pr.Employee)
                .ThenInclude(e => e.Company)
                    .ThenInclude(c => c!.City)
            .Include(pr => pr.Employee)
                .ThenInclude(e => e.Company)
                    .ThenInclude(c => c!.Documents)
            .Include(pr => pr.Employee)
                .ThenInclude(e => e.Departement)
            .Include(pr => pr.Employee)
                .ThenInclude(e => e.Contracts)
                    .ThenInclude(c => c.JobPosition)
            .Include(pr => pr.Employee)
                .ThenInclude(e => e.Contracts)
                    .ThenInclude(c => c.ContractType)
            .Include(pr => pr.Employee)
                .ThenInclude(e => e.Addresses)
                    .ThenInclude(a => a.City)
            .Include(pr => pr.Employee)
                .ThenInclude(e => e.MaritalStatus)
            .Include(pr => pr.Employee)
                .ThenInclude(e => e.Salaries)
            .Include(pr => pr.Primes)
            .FirstOrDefaultAsync(pr => pr.Id == payrollResultId, ct);

        if (payroll == null)
            return ServiceResult<byte[]>.Fail("Fiche de paie introuvable.");

        if (payroll.Status != PayrollResultStatus.OK)
            return ServiceResult<byte[]>.Fail($"Statut invalide : {payroll.Status}");

        // Solde de congés (type ANNUAL par société — aligné sur l’ancien PayslipController)
        var annualLeaveType = await _db.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.CompanyId == payroll.Employee.CompanyId
                && lt.LeaveCode == "ANNUAL"
                && lt.IsActive
                && lt.DeletedAt == null, ct);

        LeaveBalance? leaveBalance = null;
        if (annualLeaveType != null)
        {
            leaveBalance = await _db.LeaveBalances
                .AsNoTracking()
                .FirstOrDefaultAsync(lb => lb.EmployeeId == payroll.EmployeeId
                    && lb.CompanyId == payroll.Employee.CompanyId
                    && lb.Year == payroll.Year
                    && lb.Month == payroll.Month
                    && lb.LeaveTypeId == annualLeaveType.Id
                    && lb.DeletedAt == null, ct);
        }

        if (leaveBalance == null && annualLeaveType != null)
        {
            var asOfDate = new DateOnly(
                payroll.Year,
                payroll.Month,
                DateTime.DaysInMonth(payroll.Year, payroll.Month));
            var recalc = await _leaveBalanceRecalc.RecalculateAsync(
                payroll.Employee.CompanyId,
                payroll.EmployeeId,
                annualLeaveType.Id,
                asOfDate,
                userId: 0,
                ct);
            if (!recalc.Success)
                _logger.LogWarning(
                    "Recalcul solde congés impossible pour employé {EmployeeId} ({Month}/{Year}) : {Message}. PDF avec solde zéro.",
                    payroll.EmployeeId, payroll.Month, payroll.Year, recalc.ErrorMessage);
            leaveBalance = await _db.LeaveBalances
                .AsNoTracking()
                .FirstOrDefaultAsync(lb => lb.EmployeeId == payroll.EmployeeId
                    && lb.CompanyId == payroll.Employee.CompanyId
                    && lb.Year == payroll.Year
                    && lb.Month == payroll.Month
                    && lb.LeaveTypeId == annualLeaveType.Id
                    && lb.DeletedAt == null, ct);
        }

        leaveBalance ??= new LeaveBalance
        {
            EmployeeId = payroll.EmployeeId,
            CompanyId = payroll.Employee.CompanyId,
            LeaveTypeId = annualLeaveType?.Id ?? 0,
            Year = payroll.Year,
            Month = payroll.Month,
            CarryInDays = 0,
            AccruedDays = 0,
            UsedDays = 0,
            CarryOutDays = 0,
            ClosingDays = 0
        };

        if (leaveBalance.Id != 0)
            _logger.LogInformation(
                "Bulletin PDF — solde congés employé {EmployeeId} : CarryIn={CarryIn}, CarryOut={CarryOut}, Used={Used}",
                payroll.EmployeeId, leaveBalance.CarryInDays, leaveBalance.CarryOutDays, leaveBalance.UsedDays);

        // Jours ouvrables
        var workingCalendar = await _db.WorkingCalendars
            .Where(wc => wc.CompanyId == payroll.Employee.CompanyId
                && wc.IsWorkingDay
                && wc.DeletedAt == null)
            .Select(wc => wc.DayOfWeek)
            .ToListAsync(ct);

        var workingDays = workingCalendar.Any()
            ? workingCalendar.Select(d => (DayOfWeek)d).ToHashSet()
            : new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        _logger.LogInformation(
            "Jours ouvrables configurés pour société {CompanyId} : {Days}",
            payroll.Employee.CompanyId,
            string.Join(", ", workingDays));

        // Absences approuvées
        var absencesMois = await _db.EmployeeAbsences
            .Where(a => a.EmployeeId == payroll.EmployeeId
                && a.AbsenceDate.Year == payroll.Year
                && a.AbsenceDate.Month == payroll.Month
                && a.Status == AbsenceStatus.Approved
                && a.DeletedAt == null)
            .ToListAsync(ct);

        double joursAbsenceMois = absencesMois.Sum(a =>
            a.DurationType == AbsenceDurationType.HalfDay ? 0.5 : 1.0);

        int joursOuvrablesMois = CountWorkingDays(payroll.Year, payroll.Month, payroll.Month, workingDays);
        int joursTravaillesMois = (int)Math.Max(0, joursOuvrablesMois - joursAbsenceMois);

        var absencesAnnee = await _db.EmployeeAbsences
            .Where(a => a.EmployeeId == payroll.EmployeeId
                && a.AbsenceDate.Year == payroll.Year
                && a.AbsenceDate.Month <= payroll.Month
                && a.Status == AbsenceStatus.Approved
                && a.DeletedAt == null)
            .ToListAsync(ct);

        double joursAbsenceAnnee = absencesAnnee.Sum(a =>
            a.DurationType == AbsenceDurationType.HalfDay ? 0.5 : 1.0);

        int joursOuvrablesAnnee = CountWorkingDays(payroll.Year, 1, payroll.Month, workingDays);
        int joursTravaillesAnnee = (int)Math.Max(0, joursOuvrablesAnnee - joursAbsenceAnnee);

        var html = BuildPayslipHtml(payroll, leaveBalance, joursTravaillesMois, joursTravaillesAnnee, half);
        var pdfBytes = RenderPdf(html, landscape: false);

        return ServiceResult<byte[]>.Ok(pdfBytes);
    }

    // ── État CNSS PDF ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<byte[]>> GenerateEtatCnssPdfAsync(
        int companyId, int year, int month, CancellationToken ct = default)
    {
        var company = await _db.Companies.FindAsync(new object[] { companyId }, ct);
        if (company == null)
            return ServiceResult<byte[]>.Fail($"Entreprise {companyId} introuvable.");

        var results = await _db.PayrollResults
            .AsNoTracking()
            .Include(r => r.Employee)
            .Where(r => r.CompanyId == companyId
                     && r.Year == year
                     && r.Month == month
                     && r.Status == PayrollResultStatus.OK
                     && r.Employee.CnssNumber != null)
            .OrderBy(r => r.Employee.LastName)
            .ThenBy(r => r.Employee.FirstName)
            .ToListAsync(ct);

        if (!results.Any())
            return ServiceResult<byte[]>.Fail($"Aucun salarié CNSS trouvé pour {month:D2}/{year}.");

        const decimal CNSS_RG_SAL = 0.0448m;
        const decimal CNSS_AMO_SAL = 0.0226m;
        const decimal CNSS_RG_PAT = 0.0898m;
        const decimal CNSS_AF_PAT = 0.0640m;
        const decimal CNSS_FP_PAT = 0.0160m;
        const decimal CNSS_AMO_PAT = 0.0226m;
        const decimal CNSS_AMO_PAT2 = 0.0185m;

        var rows = results.Select((r, i) =>
        {
            var baseCnss = r.CnssBase ?? Math.Min(r.TotalBrut ?? 0m, 6_000m);
            var brut = r.TotalBrut ?? 0m;
            var rgSal = Math.Round(baseCnss * CNSS_RG_SAL, 2);
            var amoSal = Math.Round(brut * CNSS_AMO_SAL, 2);
            var rgPat = Math.Round(baseCnss * CNSS_RG_PAT, 2);
            var afPat = Math.Round(brut * CNSS_AF_PAT, 2);
            var fpPat = Math.Round(brut * CNSS_FP_PAT, 2);
            var amoPat = Math.Round(brut * CNSS_AMO_PAT, 2);
            var amoPat2 = Math.Round(brut * CNSS_AMO_PAT2, 2);

            return new EtatCnssFullRow
            {
                Ordre = i + 1,
                NomPrenom = $"{r.Employee.LastName} {r.Employee.FirstName}".ToUpperInvariant(),
                NumeroCnss = r.Employee.CnssNumber!,
                CIN = r.Employee.CinNumber ?? string.Empty,
                NombreJours = 26,
                SalaireBrut = brut,
                BaseCnss = baseCnss,
                RgSalarial = rgSal,
                AmoSalarial = amoSal,
                RgPatronal = rgPat,
                AfPatronal = afPat,
                FpPatronal = fpPat,
                AmoPatronal = amoPat,
                CotisationAmo = Math.Round(amoSal + amoPat, 2),
                ParticipationAmo = amoPat2
            };
        }).ToList();

        var data = new EtatCnssPdfData
        {
            CompanyName = company.CompanyName,
            CompanyCnss = company.CnssNumber ?? string.Empty,
            CompanyAddress = company.CompanyAddress ?? string.Empty,
            CompanyIce = company.IceNumber ?? string.Empty,
            Month = month,
            Year = year,
            Rows = rows
        };

        var html = BuildEtatCnssPdfHtml(data);
        var pdfBytes = RenderPdf(html, landscape: true);

        return ServiceResult<byte[]>.Ok(pdfBytes);
    }

    // ── État IR PDF ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<byte[]>> GenerateEtatIrPdfAsync(
        int companyId, int year, int month, CancellationToken ct = default)
    {
        var company = await _db.Companies.FindAsync(new object[] { companyId }, ct);
        if (company == null)
            return ServiceResult<byte[]>.Fail($"Entreprise {companyId} introuvable.");

        var results = await _db.PayrollResults
            .AsNoTracking()
            .Include(r => r.Employee)
            .Where(r => r.CompanyId == companyId
                     && r.Year == year
                     && r.Month == month
                     && r.Status == PayrollResultStatus.OK)
            .OrderBy(r => r.Employee.Matricule)
            .ThenBy(r => r.Employee.LastName)
            .ToListAsync(ct);

        if (!results.Any())
            return ServiceResult<byte[]>.Fail($"Aucun bulletin validé pour {month:D2}/{year}.");

        var data = new EtatIrPdfData
        {
            CompanyName = company.CompanyName,
            CompanyAddress = company.CompanyAddress ?? string.Empty,
            Month = month,
            Year = year,
            Rows = results.Select(r => new EtatIrFullRow
            {
                Matricule = r.Employee.Matricule ?? r.EmployeeId,
                NomPrenom = $"{r.Employee.LastName} {r.Employee.FirstName}".ToUpperInvariant(),
                SalImposable = r.BrutImposable ?? r.TotalBrut ?? 0m,
                MontantIGR = r.ImpotRevenu ?? 0m
            }).ToList()
        };

        var html = BuildEtatIrPdfHtml(data);
        var pdfBytes = RenderPdf(html, landscape: true);

        return ServiceResult<byte[]>.Ok(pdfBytes);
    }

    // ── Journal de Paie CSV ───────────────────────────────────────────────────

    public async Task<ServiceResult<byte[]>> GenerateJournalPaieCsvAsync(
        int companyId, int year, int month, CancellationToken ct = default)
    {
        var results = await _db.PayrollResults
            .Where(pr => pr.CompanyId == companyId && pr.Year == year && pr.Month == month)
            .Include(pr => pr.Employee)
            .ToListAsync(ct);

        var lines = new StringBuilder();
        lines.AppendLine("Matricule;Nom;Prénom;Salaire Base;Net à Payer");
        foreach (var pr in results)
        {
            lines.AppendLine(
                $"{pr.Employee?.Matricule};{pr.Employee?.LastName};{pr.Employee?.FirstName};" +
                $"{pr.SalaireBase};{pr.NetAPayer}");
        }

        return ServiceResult<byte[]>.Ok(Encoding.UTF8.GetBytes(lines.ToString()));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // HTML BUILDERS
    // ═════════════════════════════════════════════════════════════════════════

    private string BuildPayslipHtml(
        PayrollResult payroll,
        LeaveBalance leaveBalance,
        int joursTravaillesMois,
        int joursTravaillesAnnee,
        int? half)
    {
        var contract = GetContractForPayrollPeriod(payroll.Employee, payroll.Year, payroll.Month);
        var sb = new StringBuilder();

        // Ancienneté
        string anciennete = "N/A";
        if (contract?.StartDate != null)
        {
            var periodEnd = new DateTime(payroll.Year, payroll.Month,
                DateTime.DaysInMonth(payroll.Year, payroll.Month));
            var sen = periodEnd - contract.StartDate;
            int yrs = (int)(sen.TotalDays / 365.25);
            int mths = (int)((sen.TotalDays % 365.25) / 30.44);
            anciennete = $"{yrs} ans {mths} mois";
        }

        var primesImposables = payroll.Primes?.Where(p => p.IsTaxable).OrderBy(p => p.Ordre).ToList() ?? [];
        var indemnites = payroll.Primes?.Where(p => !p.IsTaxable).OrderBy(p => p.Ordre).ToList() ?? [];

        string irRate = "";
        if ((payroll.IrTaux ?? 0) > 0)
            irRate = $"{payroll.IrTaux!.Value * 100m:0.##}%";
        else if ((payroll.NetImposable ?? 0) > 0 && (payroll.ImpotRevenu ?? 0) > 0)
            irRate = $"{((payroll.ImpotRevenu ?? 0) / payroll.NetImposable!.Value * 100m):0.##}%";

        var frCulture = CultureInfo.GetCultureInfo("fr-FR");
        string N2(decimal v) => v.ToString("N2", frCulture);

        string cimrRate = FormatPercentRate(payroll.Employee.CimrEmployeeRate);
        string mutRate = FormatPercentRate(payroll.Employee.PrivateInsuranceRate);
        string ancienneteRate = payroll.PrimeAnciennteRate.HasValue
            ? $"{payroll.PrimeAnciennteRate * 100:0.##}%"
            : "";

        // Salaire de base « du mois » (avant prime, HS, primes imposables, excédent NI) — aligné moteur module 02–05
        decimal salaireBaseMensuel = (payroll.TotalBrut ?? 0)
            - (payroll.PrimeAnciennete ?? 0)
            - (payroll.HeuresSupp25 ?? 0) - (payroll.HeuresSupp50 ?? 0) - (payroll.HeuresSupp100 ?? 0)
            - (payroll.TotalPrimesImposables ?? 0)
            - (payroll.TotalNiExcedentImposable ?? 0);
        if (salaireBaseMensuel < 0)
            salaireBaseMensuel = 0;
        salaireBaseMensuel = Math.Round(salaireBaseMensuel, 2, MidpointRounding.AwayFromZero);

        var salairePeriode = ResolveSalaryForPayrollPeriod(payroll.Employee, payroll.Year, payroll.Month);
        decimal? tauxHoraire = salairePeriode?.BaseSalaryHourly;
        bool modeHoraire = tauxHoraire is > 0m && (salairePeriode?.BaseSalary ?? 0) <= 0m;

        // Périodicité / vue d'affichage "mensuel" vs "demi-mois".
        // - si `half` est fourni : 0 => mensuel, 1/2 => demi-mois
        // - sinon : on se base sur la périodicité société
        var periodicity = payroll.Employee.Company?.PayrollPeriodicity ?? "Mensuelle";
        var isBiMonthly = periodicity.Equals("Bimensuelle", StringComparison.OrdinalIgnoreCase);
        var demiViewRequested = half.HasValue && half.Value != 0;
        var isDemiView = half.HasValue ? demiViewRequested : isBiMonthly;
        var periodFactor = isDemiView ? 0.5m : 1m;
        var salaireBasePeriode = salaireBaseMensuel * periodFactor;

        decimal totalGains = (payroll.TotalBrut ?? 0) + (payroll.TotalIndemnites ?? 0);
        decimal leaveAvailable = leaveBalance.CarryInDays + leaveBalance.AccruedDays - leaveBalance.UsedDays;

        // Logo entreprise en base64
        string logoHtml = "";
        var companyLogo = payroll.Employee.Company?.Documents?
            .FirstOrDefault(d => d.DocumentType == "logo" && d.DeletedAt == null);
        if (companyLogo != null && !string.IsNullOrWhiteSpace(companyLogo.FilePath))
        {
            var logoPath = Path.Combine(
                _env.WebRootPath ?? _env.ContentRootPath,
                companyLogo.FilePath.TrimStart('/').TrimStart('\\'));
            if (File.Exists(logoPath))
            {
                try
                {
                    var logoBytes = File.ReadAllBytes(logoPath);
                    var logoBase64 = Convert.ToBase64String(logoBytes);
                    var ext = Path.GetExtension(companyLogo.FilePath).ToLowerInvariant();
                    var mime = ext switch
                    {
                        ".png" => "image/png",
                        ".gif" => "image/gif",
                        _ => "image/jpeg"
                    };
                    logoHtml = $"<img src='data:{mime};base64,{logoBase64}' alt='Logo' style='max-height:80px;max-width:150px;margin-bottom:8px;'/>";
                }
                catch { /* logo non critique */ }
            }
        }

        sb.Append(@"<!DOCTYPE html>
<html lang='fr'>
<head>
<meta charset='UTF-8'/>
<style>
  * { box-sizing:border-box; margin:0; padding:0; }
  body { font-family:Arial,sans-serif; font-size:8.5pt; color:#222; }
  h1   { font-size:13pt; }
  .header-row   { display:flex; justify-content:space-between; align-items:flex-start; margin-bottom:6px; }
  .header-left  { flex:2; }
  .header-right { flex:1; text-align:right; }
  .header-right .title  { font-size:15pt; font-weight:bold; }
  .header-right .period { font-size:10pt; font-weight:bold; margin-top:4px; }
  hr.sep { border:none; border-top:1px solid #bbb; margin:8px 0; }
  .emp-box { background:#f0f0f0; padding:10px; margin-bottom:8px; }
  .emp-row { display:flex; gap:6px; margin-bottom:4px; }
  .emp-row > div { flex:1; }
  table { width:100%; border-collapse:collapse; margin-bottom:8px; font-size:8pt; }
  table th { background:#1a3c6e; color:#fff; padding:5px 4px; text-align:right; font-size:8.5pt; }
  table th.label-col { text-align:left; }
  table td { padding:3px 4px; border-bottom:0.5px solid #ddd; }
  table td.label-col { text-align:left; }
  table td.right-col { text-align:right; }
  table tr.sep-row td { padding:1px 0; border-bottom:1.5px solid #999; }
  .summary-table th { font-size:7.5pt; background:#555; }
  .summary-table td { text-align:center; font-size:8pt; }
  .conge-box { background:#e8f0fe; padding:8px; margin-top:8px; }
  .conge-box .conge-title { font-weight:bold; font-size:9pt; margin-bottom:6px; }
  .conge-row { display:flex; gap:8px; }
  .conge-row > div { flex:1; }
  .footer { text-align:center; font-size:7pt; color:#777; margin-top:10px; border-top:0.5px solid #ddd; padding-top:4px; }
</style>
</head>
<body>
");
        sb.Append($@"
<div class='header-row'>
  <div class='header-left'>
    {logoHtml}
    <h1>{H(payroll.Employee.Company?.CompanyName)}</h1>
    <div>ICE N° : {H(payroll.Employee.Company?.IceNumber ?? "N/A")}</div>
    <div>CNSS N° : {H(payroll.Employee.Company?.CnssNumber ?? "N/A")}</div>
    <div>IF N° : {H(payroll.Employee.Company?.IfNumber ?? "N/A")}</div>
    <div>Adresse : {H(payroll.Employee.Company?.CompanyAddress ?? "N/A")}</div>
    <div><b>{H(payroll.Employee.Company?.City?.CityName ?? "N/A")}</b></div>
  </div>
  <div class='header-right'>
    <div class='title'>BULLETIN DE PAIE</div>
    <div class='period'>Période : {GetMonthName(payroll.Month)} {payroll.Year}</div>
  </div>
</div>
<hr class='sep'/>
");
        string matricule = payroll.Employee.Matricule?.ToString() ?? payroll.EmployeeId.ToString();
        string dateEmbauche = contract?.StartDate != null ? contract.StartDate.ToString("dd/MM/yyyy") : "N/A";
        string cimrDiv = !string.IsNullOrWhiteSpace(payroll.Employee.CimrNumber) ? $"<div>CIMR : {payroll.Employee.CimrNumber}</div>" : "";
        string mutuelleDiv = !string.IsNullOrWhiteSpace(payroll.Employee.PrivateInsuranceNumber) ? $"<div>Mutuelle : {payroll.Employee.PrivateInsuranceNumber}</div>" : "";
        string periodLabel = $"{DateTime.DaysInMonth(payroll.Year, payroll.Month):D2}/{payroll.Month:D2}/{payroll.Year}";
        var primaryAddress = payroll.Employee.Addresses?
            .FirstOrDefault(a => a.DeletedAt == null);
        string adresse = primaryAddress != null
            ? $"{primaryAddress.AddressLine1}{(string.IsNullOrEmpty(primaryAddress.AddressLine2) ? "" : ", " + primaryAddress.AddressLine2)}, {primaryAddress.ZipCode} {primaryAddress.City?.CityName ?? ""}".Trim().TrimEnd(',')
            : "N/A";
        string contractTypeName = H(contract?.ContractType?.ContractTypeName ?? "N/A");
        string paymentMethod = H(payroll.Employee.Company?.PaymentMethod ?? "N/A");

        sb.Append($@"
<div class='emp-box'>
  <div class='emp-row'>
    <div><b>Nom : {H(payroll.Employee.FirstName)} {H(payroll.Employee.LastName)}</b></div>
    <div>CIN : {H(payroll.Employee.CinNumber ?? "N/A")}</div>
    <div>CNSS : {H(payroll.Employee.CnssNumber ?? "N/A")}</div>
    {cimrDiv}
    {mutuelleDiv}
  </div>
  <div class='emp-row'>
    <div>Matricule : {matricule}</div>
    <div>Département : {H(payroll.Employee.Departement?.DepartementName ?? "N/A")}</div>
    <div>Fonction : {H(contract?.JobPosition?.Name ?? "N/A")}</div>
    <div>Situation fam. : {H(payroll.Employee.MaritalStatus?.NameFr ?? "N/A")}</div>
  </div>
  <div class='emp-row'>
    <div>Date d'embauche : {dateEmbauche}</div>
    <div>Ancienneté : {anciennete}</div>
    <div>Date naissance : {payroll.Employee.DateOfBirth:dd/MM/yyyy}</div>
    <div><b>Période : {periodLabel}</b></div>
  </div>
  <div class='emp-row'>
    <div>Type de contrat : {contractTypeName}</div>
    <div colspan='3'>Adresse : {H(adresse)}</div>
  </div>
</div>
");
        sb.Append(@"
<table>
  <thead>
    <tr>
      <th class='label-col' style='width:42%'>LIBELLÉ</th>
      <th style='width:14%'>BASE</th>
      <th style='width:10%'>TAUX</th>
      <th style='width:17%'>GAIN</th>
      <th style='width:17%'>RETENUE</th>
    </tr>
  </thead>
  <tbody>
");
        // Salaire de base : au forfait → BASE = salaire contractuel, GAIN = montant ; à l'heure → BASE = heures, TAUX = DH/h, GAIN = montant
        string baseSalaire, tauxSalaire, gainSalaire;
        if (modeHoraire)
        {
            var th = tauxHoraire!.Value;
            var heures = th > 0m
                ? Math.Round(salaireBasePeriode / th, 2, MidpointRounding.AwayFromZero)
                : 0m;
            baseSalaire = N2(heures);
            tauxSalaire = N2(th);
            gainSalaire = N2(salaireBasePeriode);
        }
        else
        {
            baseSalaire = (payroll.SalaireBase ?? 0) > 0 ? N2(payroll.SalaireBase!.Value * periodFactor) : "";
            tauxSalaire = "";
            gainSalaire = N2(salaireBasePeriode);
        }

        string salaireBaseLabel;
        if (!isDemiView)
        {
            salaireBaseLabel = "Salaire de base (mensuel)";
        }
        else
        {
            int? halfVal = half;
            salaireBaseLabel = halfVal switch
            {
                1 => "Salaire de base (1-15)",
                2 => "Salaire de base (16-31)",
                _ => "Salaire de base (demi-mois)"
            };
        }
        TR(sb, salaireBaseLabel, baseSalaire, tauxSalaire, gainSalaire, "");
        if ((payroll.HeuresSupp25 ?? 0) > 0)
            TR(sb, "Heures supplémentaires 25%", "", "25%", F(payroll.HeuresSupp25), "");
        if ((payroll.HeuresSupp50 ?? 0) > 0)
            TR(sb, "Heures supplémentaires 50%", "", "50%", F(payroll.HeuresSupp50), "");
        if ((payroll.HeuresSupp100 ?? 0) > 0)
            TR(sb, "Heures supplémentaires 100%", "", "100%", F(payroll.HeuresSupp100), "");
        if ((payroll.Conges ?? 0) > 0)
            TR(sb, "Congés payés", "", "", F(payroll.Conges), "");
        if ((payroll.JoursFeries ?? 0) > 0)
            TR(sb, "Jours fériés", "", "", F(payroll.JoursFeries), "");
        if ((payroll.PrimeAnciennete ?? 0) > 0)
        {
            var basePrime = modeHoraire
                ? N2(salaireBasePeriode)
                : (payroll.SalaireBase ?? 0) > 0
                    ? N2(payroll.SalaireBase!.Value * periodFactor)
                    : N2(salaireBasePeriode);

            string primeAncLabel;
            if (!isDemiView)
            {
                primeAncLabel = "Prime d'ancienneté";
            }
            else
            {
                int? halfVal = half;
                primeAncLabel = halfVal switch
                {
                    1 => "Prime d'ancienneté (1-15)",
                    2 => "Prime d'ancienneté (16-31)",
                    _ => "Prime d'ancienneté (demi-mois)"
                };
            }
            var primeAncMontant = payroll.PrimeAnciennete!.Value * periodFactor;
            TR(sb, primeAncLabel, basePrime, ancienneteRate, N2(primeAncMontant), "");
        }
        foreach (var p in primesImposables)
            TR(sb, H(p.Label), "", "", p.Montant.ToString("N2"), "");
        if (!primesImposables.Any())
        {
            if ((payroll.PrimeImposable1 ?? 0) > 0)
                TR(sb, "Prime imposable 1", "", "", F(payroll.PrimeImposable1), "");
            if ((payroll.PrimeImposable2 ?? 0) > 0)
                TR(sb, "Prime imposable 2", "", "", F(payroll.PrimeImposable2), "");
            if ((payroll.PrimeImposable3 ?? 0) > 0)
                TR(sb, "Prime imposable 3", "", "", F(payroll.PrimeImposable3), "");
        }
        if ((payroll.TotalNiExcedentImposable ?? 0) > 0)
            TR(sb, "Excédent indemnités (imposable)", "", "", F(payroll.TotalNiExcedentImposable), "");

        TRSep(sb);
        TR(sb, "SALAIRE BRUT IMPOSABLE", "", "", N2(payroll.TotalBrut ?? 0), "", bold: true);
        TRSep(sb);

        if ((payroll.FraisProfessionnels ?? 0) > 0)
            TR(sb, "Frais professionnels", F(payroll.BrutImposable), "25%", "", F(payroll.FraisProfessionnels));

        foreach (var ind in indemnites)
            TR(sb, $"{H(ind.Label)} (NI)", "", "", ind.Montant.ToString("N2"), "");
        if ((payroll.IndemniteRepresentation ?? 0) > 0)
            TR(sb, "Indemnité de représentation (NI)", "", "", F(payroll.IndemniteRepresentation), "");
        if ((payroll.PrimeTransport ?? 0) > 0)
            TR(sb, "Prime de transport (NI)", "", "", F(payroll.PrimeTransport), "");
        if ((payroll.PrimePanier ?? 0) > 0)
            TR(sb, "Prime de panier (NI)", "", "", F(payroll.PrimePanier), "");
        if ((payroll.IndemniteDeplacement ?? 0) > 0)
            TR(sb, "Indemnité de déplacement (NI)", "", "", F(payroll.IndemniteDeplacement), "");
        if ((payroll.IndemniteCaisse ?? 0) > 0)
            TR(sb, "Indemnité de caisse (NI)", "", "", F(payroll.IndemniteCaisse), "");
        if ((payroll.PrimeSalissure ?? 0) > 0)
            TR(sb, "Prime de salissure (NI)", "", "", F(payroll.PrimeSalissure), "");
        if ((payroll.GratificationsFamilial ?? 0) > 0)
            TR(sb, "Gratifications familiales (NI)", "", "", F(payroll.GratificationsFamilial), "");
        if ((payroll.PrimeVoyageMecque ?? 0) > 0)
            TR(sb, "Prime de voyage à la Mecque (NI)", "", "", F(payroll.PrimeVoyageMecque), "");
        if ((payroll.IndemniteLicenciement ?? 0) > 0)
            TR(sb, "Indemnité de licenciement (NI)", "", "", F(payroll.IndemniteLicenciement), "");
        if ((payroll.IndemniteKilometrique ?? 0) > 0)
            TR(sb, "Indemnité kilométrique (NI)", "", "", F(payroll.IndemniteKilometrique), "");
        if ((payroll.PrimeTourne ?? 0) > 0)
            TR(sb, "Prime de tournée (NI)", "", "", F(payroll.PrimeTourne), "");
        if ((payroll.PrimeOutillage ?? 0) > 0)
            TR(sb, "Prime d'outillage (NI)", "", "", F(payroll.PrimeOutillage), "");
        if ((payroll.AideMedicale ?? 0) > 0)
            TR(sb, "Aide médicale (NI)", "", "", F(payroll.AideMedicale), "");
        if ((payroll.AutresPrimesNonImposable ?? 0) > 0)
            TR(sb, "Autres primes non imposables (NI)", "", "", F(payroll.AutresPrimesNonImposable), "");

        TRSep(sb);

        if ((payroll.CnssPartSalariale ?? 0) > 0)
            TR(sb, "CNSS (part salariale)", F(payroll.CnssBase ?? payroll.BrutImposable), "4.48%", "", F(payroll.CnssPartSalariale));
        if ((payroll.CimrPartSalariale ?? 0) > 0)
            TR(sb, "CIMR (part salariale)", F(payroll.CimrBase ?? payroll.BrutImposable), cimrRate, "", F(payroll.CimrPartSalariale));
        if ((payroll.AmoPartSalariale ?? 0) > 0)
            TR(sb, "AMO (part salariale)", F(payroll.AmoBase ?? payroll.BrutImposable), "2.26%", "", F(payroll.AmoPartSalariale));
        if ((payroll.MutuellePartSalariale ?? 0) > 0)
            TR(sb, "Mutuelle (part salariale)", F(payroll.MutuelleBase ?? payroll.BrutImposable), mutRate, "", F(payroll.MutuellePartSalariale));
        if ((payroll.ImpotRevenu ?? 0) > 0)
            TR(sb, "Impôt sur le revenu (IR)", F(payroll.NetImposable), irRate, "", F(payroll.ImpotRevenu));
        if ((payroll.Arrondi ?? 0) != 0)
            TR(sb, "Arrondi", "", "", "", F(payroll.Arrondi));
        if ((payroll.AvanceSurSalaire ?? 0) > 0)
            TR(sb, "Avance sur salaire", "", "", "", F(payroll.AvanceSurSalaire));
        if ((payroll.InteretSurLogement ?? 0) > 0)
            TR(sb, "Intérêt sur logement", "", "", "", F(payroll.InteretSurLogement));

        TRSep(sb);
        TR(sb, "TOTAL GAINS", "", "", totalGains.ToString("N2"), "", bold: true);
        TR(sb, "TOTAL RETENUES", "", "", "", F(payroll.TotalRetenues) ?? "0.00", bold: true);
        TRSep(sb);

        sb.Append($@"
    <tr>
      <td colspan='4' class='label-col' style='background:#c8e6c9;font-weight:bold;font-size:11pt;padding:6px'>NET À PAYER</td>
      <td class='right-col' style='background:#b9dfbb;font-weight:bold;font-size:11pt;padding:6px'>{F(payroll.NetAPayer)} MAD</td>
    </tr>
  </tbody>
</table>
");
        sb.Append($@"
<table class='summary-table'>
  <thead>
    <tr>
      <th>CNSS Pat.</th><th>AMO Pat.</th><th>CIMR Pat.</th><th>Mutuelle Pat.</th><th>Brut Imposable</th><th>Net Imposable</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>{F(payroll.CnssPartPatronale) ?? "0.00"}</td>
      <td>{F(payroll.AmoPartPatronale) ?? "0.00"}</td>
      <td>{F(payroll.CimrPartPatronale) ?? "0.00"}</td>
      <td>{F(payroll.MutuellePartPatronale) ?? "0.00"}</td>
      <td>{F(payroll.BrutImposable) ?? "0.00"}</td>
      <td>{F(payroll.NetImposable) ?? "0.00"}</td>
    </tr>
  </tbody>
  <thead>
    <tr>
      <th>IR</th><th>Total Cotis. Sal.</th><th>Total Cotis. Pat.</th><th>Total Retenues</th><th>Avance Salaire</th><th>Intérêt Logement</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>{F(payroll.ImpotRevenu) ?? "0.00"}</td>
      <td>{F(payroll.TotalCotisationsSalariales) ?? "0.00"}</td>
      <td>{F(payroll.TotalCotisationsPatronales) ?? "0.00"}</td>
      <td>{F(payroll.TotalRetenues) ?? "0.00"}</td>
      <td>{F(payroll.AvanceSurSalaire) ?? "0.00"}</td>
      <td>{F(payroll.InteretSurLogement) ?? "0.00"}</td>
    </tr>
  </tbody>
</table>
");
        sb.Append($@"
<div class='conge-box'>
  <div class='conge-title'>SOLDE DE CONGÉS</div>
  <div class='conge-row'>
    <div>
      <div>Solde reporté (N-1) : {leaveBalance.CarryInDays:N2} j</div>
      <div>Acquis ce mois : {leaveBalance.AccruedDays:N2} j</div>
    </div>
    <div>
      <div>Jours pris : {leaveBalance.UsedDays:N2} j</div>
      <div>Solde reporté (N+1) : {leaveBalance.CarryOutDays:N2} j</div>
    </div>
    <div>
      <div><b>Solde disponible : {leaveAvailable:N2} j</b></div>
    </div>
  </div>
</div>
");
        sb.Append($@"
<div style='background:#fff8e1;padding:8px;margin-top:8px;'>
  <div style='font-weight:bold;font-size:9pt;margin-bottom:6px;'>PRÉSENCE &amp; PAIEMENT</div>
  <div style='display:flex;gap:8px;'>
    <div style='flex:1'><div>Moyen de paiement : <b>{paymentMethod}</b></div></div>
    <div style='flex:1'><div>Jours travaillés ce mois : <b>{joursTravaillesMois} j</b></div></div>
    <div style='flex:1'><div>Cumul jours travaillés ({payroll.Year}) : <b>{joursTravaillesAnnee} j</b></div></div>
  </div>
</div>
");
        sb.Append($@"
<div style='margin-top:20px;display:flex;justify-content:space-between;align-items:flex-start;'>
  <div style='flex:1;text-align:left;'>
    <div style='font-size:9pt;margin-bottom:4px;'>Fait à <b>{H(payroll.Employee.Company?.City?.CityName ?? "N/A")}</b></div>
    <div style='font-size:9pt;margin-bottom:30px;'>Le <b>{DateTime.Now:dd/MM/yyyy}</b></div>
    <div style='font-size:9pt;font-weight:bold;margin-bottom:4px;'>Signature de l'employeur</div>
    <div style='font-size:8pt;color:#555;margin-top:40px;border-top:1px solid #999;padding-top:4px;max-width:200px;'>{H(payroll.Employee.Company?.SignatoryName ?? "N/A")}</div>
  </div>
</div>
</body>
</html>
");
        return sb.ToString();
    }

    // ── HTML État CNSS ────────────────────────────────────────────────────────

    private static string BuildEtatCnssPdfHtml(EtatCnssPdfData d)
    {
        var rows = d.Rows ?? [];

        // Taille de page « contrôlée » : on découpe la liste pour que les totaux
        // (page actuelle vs cumul) correspondent réellement au contenu de chaque page.
        const int rowsPerPage = 20;
        var totalPages = rows.Count == 0
            ? 1
            : (int)Math.Ceiling(rows.Count / (double)rowsPerPage);

        // ── totaux MOIS (récapitulatif final) ────────────────────────────────
        decimal totalBrut = rows.Sum(r => r.SalaireBrut);
        decimal totalBaseCnss = rows.Sum(r => r.BaseCnss);
        decimal totalRgSal = rows.Sum(r => r.RgSalarial);
        decimal totalAmoSal = rows.Sum(r => r.AmoSalarial);
        decimal totalRgPat = rows.Sum(r => r.RgPatronal);
        decimal totalAfPat = rows.Sum(r => r.AfPatronal);
        decimal totalFpPat = rows.Sum(r => r.FpPatronal);
        decimal totalAmoPat = rows.Sum(r => r.AmoPatronal);
        decimal totalCotisAmo = rows.Sum(r => r.CotisationAmo);
        decimal totalParticipAmo = rows.Sum(r => r.ParticipationAmo);
        decimal totalPS = totalRgSal + totalRgPat;
        decimal totalAmo = totalCotisAmo + totalParticipAmo;
        decimal totalAPayer = totalPS + totalAfPat + totalFpPat;
        decimal totalGeneral = totalAPayer + totalAmo;

        // ── génération tables page par page ───────────────────────────────
        decimal cumulBrut = 0m;
        decimal cumulBaseCnss = 0m;
        var tablesSb = new StringBuilder();

        for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
        {
            var pageRows = rows
                .Skip(pageIndex * rowsPerPage)
                .Take(rowsPerPage)
                .ToList();

            if (pageRows.Count == 0)
                break;

            decimal pageBrut = pageRows.Sum(r => r.SalaireBrut);
            decimal pageBaseCnss = pageRows.Sum(r => r.BaseCnss);

            cumulBrut += pageBrut;
            cumulBaseCnss += pageBaseCnss;

            var bodySb = new StringBuilder();
            foreach (var r in pageRows)
            {
                var parts = r.NomPrenom.Split(' ', 2);
                var nom = parts.Length > 0 ? parts[0] : "";
                var prenom = parts.Length > 1 ? parts[1] : "";
                bodySb.Append($@"
<tr>
  <td class='c'>{r.NumeroCnss}</td>
  <td class='l name'>{nom}</td>
  <td class='l name'>{prenom}</td>
  <td class='c'>{r.NombreJours}</td>
  <td class='r'>{N(r.SalaireBrut)}</td>
  <td class='r'>{N(r.BaseCnss)}</td>
</tr>");
            }

            tablesSb.Append($@"
<table>
<thead>
  <tr>
    <th class=""c"" style=""width:15%"">Num CNSS</th>
    <th class=""l"" style=""width:20%"">NOM</th>
    <th class=""l"" style=""width:20%"">Prénom</th>
    <th class=""c"" style=""width:10%"">NBR jours</th>
    <th class=""r"" style=""width:17.5%"">SAL.BRUT.IMP</th>
    <th class=""r"" style=""width:17.5%"">SAL.PLAF</th>
  </tr>
</thead>
<tbody>
{bodySb}
</tbody>
<tfoot>
  <tr class=""tot-page"">
    <td colspan=""4"" class=""l"">TOTAL PAGE ACTUELLE</td>
    <td class=""r"">{N(pageBrut)}</td>
    <td class=""r"">{N(pageBaseCnss)}</td>
  </tr>
  <tr class=""tot-cumul"">
    <td colspan=""4"" class=""l"">TOTAL CUMUL PAGE ACTUELLE ET PRECEDENTES</td>
    <td class=""r"">{N(cumulBrut)}</td>
    <td class=""r"">{N(cumulBaseCnss)}</td>
  </tr>
</tfoot>
</table>");

            if (pageIndex < totalPages - 1)
                tablesSb.Append("<div style='page-break-after:always'></div>");
        }

        return $@"<!DOCTYPE html>
<html lang=""fr"">
<head>
<meta charset=""UTF-8""/>
<style>
  * {{ margin:0; padding:0; box-sizing:border-box; }}
  body {{ font-family:'Arial',sans-serif; font-size:8pt; color:#1a1a1a; }}
  .header {{ text-align:center; margin-bottom:12px; }}
  .doc-title {{ font-size:11pt; font-weight:bold; }}
  table {{ width:100%; border-collapse:collapse; font-size:9pt; }}
  thead th {{ background:#fff; color:#000; padding:6px 8px; border:1px solid #000; text-align:center; white-space:nowrap; font-size:8pt; font-weight:bold; }}
  tbody tr {{ background:#fff; }}
  td {{ padding:4px 8px; border:1px solid #000; vertical-align:middle; }}
  .c {{ text-align:center; }} .l {{ text-align:left; }} .r {{ text-align:right; font-variant-numeric:tabular-nums; }}
  .name {{ font-weight:normal; }}
  tr.tot-page td, tr.tot-cumul td {{ background:#fff; color:#000; font-weight:bold; border:1px solid #000; padding:6px 8px; text-transform:uppercase; }}
  .recap {{ margin-top:16px; border:1px solid #000; }}
  .recap-section {{ padding:6px 10px; border-bottom:1px solid #000; background:#fff; }}
  .recap-section:last-child {{ border-bottom:none; }}
  .recap-section .sec-title {{ font-weight:bold; font-size:8.5pt; margin-bottom:3px; text-transform:uppercase; }}
  .recap-section .line {{ display:flex; justify-content:space-between; align-items:center; padding:2px 0; font-size:8pt; }}
  .recap-section .line.sub {{ padding-left:16px; font-size:7.5pt; color:#333; }}
  .recap-section .line .label {{ flex:1; }}
  .recap-section .line .taux {{ color:#555; font-size:7pt; margin-right:8px; }}
  .recap-section .line .val {{ font-weight:bold; min-width:90px; text-align:right; }}
  .recap-section.total-final {{ border-top:2px solid #000; }}
  .recap-section.total-final .line {{ font-size:9pt; font-weight:bold; }}
  .recap-section.total-final .val {{ font-size:10pt; }}
</style>
</head>
<body>
<div class=""header"">
  <div class=""doc-title"">ETAT CNSS DU MOIS : {d.Month:D2}/{d.Year.ToString().Substring(2)}</div>
</div>
{tablesSb}
<div class=""recap"">
  <div class=""recap-section"">
    <div class=""sec-title"">Allocations Familiales</div>
    <div class=""line""><span class=""label"">Part patronale</span><span class=""taux"">(6,40 % sur brut)</span><span class=""val"">{N(totalAfPat)}</span></div>
  </div>
  <div class=""recap-section"">
    <div class=""sec-title"">Prestations Sociales</div>
    <div class=""line""><span class=""label"">Part salariale</span><span class=""taux"">(4,48 % sur base plafonnée)</span><span class=""val"">{N(totalRgSal)}</span></div>
    <div class=""line""><span class=""label"">Part patronale</span><span class=""taux"">(8,98 % sur base plafonnée)</span><span class=""val"">{N(totalRgPat)}</span></div>
    <div class=""line sub""><span class=""label""><strong>Total Prestations Sociales</strong></span><span class=""taux"">(13,46 %)</span><span class=""val""><strong>{N(totalPS)}</strong></span></div>
  </div>
  <div class=""recap-section"">
    <div class=""sec-title"">Formation Professionnelle</div>
    <div class=""line""><span class=""label"">Part patronale</span><span class=""taux"">(1,60 % sur brut)</span><span class=""val"">{N(totalFpPat)}</span></div>
  </div>
  <div class=""recap-section"">
    <div class=""line"" style=""font-weight:bold;font-size:8.5pt""><span class=""label"">TOTAL À PAYER</span><span class=""taux""></span><span class=""val"" style=""font-size:9pt"">{N(totalAPayer)}</span></div>
  </div>
  <div class=""recap-section"">
    <div class=""sec-title"">Assurance Maladie Obligatoire</div>
    <div class=""line""><span class=""label"">Cotisation A.M.O</span><span class=""taux"">(4,52 % sur brut)</span><span class=""val"">{N(totalCotisAmo)}</span></div>
    <div class=""line sub""><span class=""label"">→ dont part salariale</span><span class=""taux"">(2,26 %)</span><span class=""val"">{N(totalAmoSal)}</span></div>
    <div class=""line sub""><span class=""label"">→ dont part patronale</span><span class=""taux"">(2,26 %)</span><span class=""val"">{N(totalCotisAmo - totalAmoSal)}</span></div>
    <div class=""line""><span class=""label"">Participation A.M.O</span><span class=""taux"">(1,85 % sur brut)</span><span class=""val"">{N(totalParticipAmo)}</span></div>
    <div class=""line sub""><span class=""label""><strong>Total A.M.O</strong></span><span class=""taux"">(6,37 %)</span><span class=""val""><strong>{N(totalAmo)}</strong></span></div>
  </div>
  <div class=""recap-section total-final"">
    <div class=""line""><span class=""label"">TOTAL</span><span class=""taux""></span><span class=""val"">{N(totalGeneral)}</span></div>
  </div>
</div>
</body>
</html>";
    }

    // ── HTML État IR ──────────────────────────────────────────────────────────

    private static string BuildEtatIrPdfHtml(EtatIrPdfData d)
    {
        decimal totalSalImposable = d.Rows.Sum(r => r.SalImposable);
        decimal totalIGR = d.Rows.Sum(r => r.MontantIGR);

        var sb = new StringBuilder();
        foreach (var r in d.Rows)
        {
            sb.Append($@"
<tr>
  <td class='c'>{r.Matricule}</td>
  <td class='l'>{r.NomPrenom}</td>
  <td class='r'>{N(r.SalImposable)}</td>
  <td class='r'>{N(r.MontantIGR)}</td>
</tr>");
        }

        return $@"<!DOCTYPE html>
<html lang=""fr"">
<head>
<meta charset=""UTF-8""/>
<style>
  * {{ margin:0; padding:0; box-sizing:border-box; }}
  body {{ font-family:'Arial',sans-serif; font-size:9pt; color:#000; }}
  .company-header {{ margin-bottom:16px; font-size:8.5pt; line-height:1.4; }}
  .company-name {{ font-weight:bold; }}
  .title-row {{ display:flex; justify-content:center; align-items:center; position:relative; margin-bottom:12px; }}
  .doc-title {{ font-size:11pt; font-weight:bold; text-align:center; }}
  .page-num {{ position:absolute; right:0; font-size:9pt; }}
  table {{ width:100%; border-collapse:collapse; font-size:9pt; margin-top:8px; }}
  thead th {{ background:#fff; color:#000; padding:6px 8px; border:1px solid #000; text-align:center; font-weight:bold; font-size:8.5pt; }}
  tbody tr {{ background:#fff; }}
  td {{ padding:5px 8px; border:1px solid #000; vertical-align:middle; }}
  .c {{ text-align:center; }} .l {{ text-align:left; }} .r {{ text-align:right; font-variant-numeric:tabular-nums; }}
  tfoot td {{ background:#fff; color:#000; font-weight:normal; border:1px solid #000; padding:6px 8px; }}
</style>
</head>
<body>
<div class=""company-header"">
  <div class=""company-name"">{d.CompanyName}</div>
  <div>{d.CompanyAddress}</div>
</div>
<div class=""title-row"">
  <div class=""doc-title"">ETAT DES PRELEVEMENTS DU MOIS &nbsp; {d.Month:D2} / {d.Year.ToString().Substring(2)}</div>
  <div class=""page-num"">1/1</div>
</div>
<table>
<thead>
  <tr>
    <th class=""c"" style=""width:12%"">Matricule</th>
    <th class=""l"" style=""width:48%"">NOM</th>
    <th class=""r"" style=""width:20%"">SAL IMPOS</th>
    <th class=""r"" style=""width:20%"">MONTANT IGR</th>
  </tr>
</thead>
<tbody>
{sb}
</tbody>
<tfoot>
  <tr>
    <td colspan=""2"" class=""r"" style=""border-top:2px solid #000""></td>
    <td class=""r"" style=""border-top:2px solid #000"">{N(totalSalImposable)}</td>
    <td class=""r"" style=""border-top:2px solid #000"">{N(totalIGR)}</td>
  </tr>
</tfoot>
</table>
</body>
</html>";
    }

    // ═════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    private static byte[] RenderPdf(string html, bool landscape)
    {
        var renderer = new ChromePdfRenderer();
        renderer.RenderingOptions.PaperSize = IronPdf.Rendering.PdfPaperSize.A4;
        renderer.RenderingOptions.PaperOrientation = landscape
            ? IronPdf.Rendering.PdfPaperOrientation.Landscape
            : IronPdf.Rendering.PdfPaperOrientation.Portrait;
        renderer.RenderingOptions.MarginTop = 15;
        renderer.RenderingOptions.MarginBottom = 15;
        renderer.RenderingOptions.MarginLeft = 15;
        renderer.RenderingOptions.MarginRight = 15;
        renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print;

        var doc = renderer.RenderHtmlAsPdf(html);
        return doc.BinaryData;
    }

    private static string H(string? s) =>
        System.Net.WebUtility.HtmlEncode(s ?? "");

    private static string? F(decimal? v) => v?.ToString("N2");

    private static string N(decimal v) =>
        v.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));

    private static void TR(StringBuilder sb, string label, string? baseVal, string? rate,
                            string? gain, string? retenue, bool bold = false)
    {
        var b = bold ? " font-weight:bold;" : "";
        sb.Append($@"
    <tr>
      <td class='label-col' style='{b}'>{label}</td>
      <td class='right-col' style='{b}'>{baseVal ?? ""}</td>
      <td class='right-col' style='{b}'>{rate ?? ""}</td>
      <td class='right-col' style='{b}'>{gain ?? ""}</td>
      <td class='right-col' style='{b}'>{retenue ?? ""}</td>
    </tr>");
    }

    private static void TRSep(StringBuilder sb) =>
        sb.Append(@"
    <tr class='sep-row'><td colspan='5' style='padding:1px 0;border-bottom:1.5px solid #999'></td></tr>");

    /// <summary>Contrat actif à la période de paie (chevauchement du mois), le plus récent en cas de chevauchement.</summary>
    private static EmployeeContract? GetContractForPayrollPeriod(Payzen.Domain.Entities.Employee.Employee? employee, int year, int month)
    {
        var contracts = employee?.Contracts;
        if (contracts == null || contracts.Count == 0)
            return null;
        var end = new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
        var start = new DateTime(year, month, 1).Date;
        return contracts
            .Where(c => c.StartDate.Date <= end && (c.EndDate == null || c.EndDate.Value.Date >= start))
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefault();
    }

    /// <summary>Barème salarial actif sur le mois de paie (taux horaire / salaire mensuel).</summary>
    private static EmployeeSalary? ResolveSalaryForPayrollPeriod(Payzen.Domain.Entities.Employee.Employee? employee, int year, int month)
    {
        var list = employee?.Salaries;
        if (list == null || list.Count == 0)
            return null;
        var end = new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
        var start = new DateTime(year, month, 1).Date;
        return list
            .Where(s => s.DeletedAt == null)
            .Where(s => s.EffectiveDate.Date <= end && (s.EndDate == null || s.EndDate.Value.Date >= start))
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefault();
    }

    /// <summary>Taux affichés sur le PDF : accepte fraction (0,045) ou pourcentage saisi (4,5).</summary>
    private static string FormatPercentRate(decimal? rateNullable)
    {
        if (!rateNullable.HasValue)
            return "";
        var r = rateNullable.Value;
        if (r > 1m && r <= 100m)
            return $"{r:0.##}%";
        return $"{r * 100m:0.##}%";
    }

    private static int CountWorkingDays(int year, int monthFrom, int monthTo, HashSet<DayOfWeek> workingDays)
    {
        int count = 0;
        for (int m = monthFrom; m <= monthTo; m++)
        {
            int daysInMonth = DateTime.DaysInMonth(year, m);
            for (int d = 1; d <= daysInMonth; d++)
            {
                if (workingDays.Contains(new DateTime(year, m, d).DayOfWeek))
                    count++;
            }
        }
        return count;
    }

    private static string GetMonthName(int month) => month switch
    {
        1 => "Janvier",
        2 => "Février",
        3 => "Mars",
        4 => "Avril",
        5 => "Mai",
        6 => "Juin",
        7 => "Juillet",
        8 => "Août",
        9 => "Septembre",
        10 => "Octobre",
        11 => "Novembre",
        12 => "Décembre",
        _ => "Inconnu"
    };
}
