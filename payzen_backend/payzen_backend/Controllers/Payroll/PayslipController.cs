using IronPdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Leave;
using payzen_backend.Models.LeaveBalance;
using payzen_backend.Models.Payroll;
using payzen_backend.Services.Leave;
using System.Text;

namespace payzen_backend.Controllers.Payroll
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PayslipController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PayslipController> _logger;
        private readonly LeaveBalanceService _leaveBalanceService;
        private readonly IWebHostEnvironment _environment;

        public PayslipController(
            AppDbContext db, 
            ILogger<PayslipController> logger, 
            LeaveBalanceService leaveBalanceService,
            IWebHostEnvironment environment)
        {
            _db = db;
            _logger = logger;
            _leaveBalanceService = leaveBalanceService;
            _environment = environment;
        }

        /// <summary>
        /// Génère une fiche de paie en PDF pour un employé et une période donnée.
        /// GET: api/payslip/employee/{employeeId}/period/{year}/{month}
        /// </summary>
        [HttpGet("employee/{employeeId}/period/{year}/{month}")]
        public async Task<IActionResult> GeneratePayslipPdf(int employeeId, int year, int month)
        {
            try
            {
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(emp => emp.Id == employeeId);

                int lastDay = DateTime.DaysInMonth(year, month);
                DateOnly asOfDate = new DateOnly(year, month, lastDay);
                _logger.LogInformation("Date of the payslip is : {Year} - {Month} - {LastDay}", year, month, lastDay);

                var payrollResult = await _db.PayrollResults
                    .Include(pr => pr.Employee)
                        .ThenInclude(e => e.Company)
                            .ThenInclude(c => c.City)
                    .Include(pr => pr.Employee)
                        .ThenInclude(e => e.Company)
                            .ThenInclude(c => c.Documents)
                    .Include(pr => pr.Employee)
                        .ThenInclude(e => e.Departement)
                    .Include(pr => pr.Employee)
                        .ThenInclude(e => e.Contracts)
                            .ThenInclude(c => c.JobPosition)
                    .Include(pr => pr.Primes)
                    .FirstOrDefaultAsync(pr => pr.EmployeeId == employeeId
                        && pr.Year == year
                        && pr.Month == month
                        && pr.DeletedAt == null);

                var leaveBalance = await _db.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId
                        && lb.Year == year
                        && lb.Month == month
                        && lb.DeletedAt == null);

                if (payrollResult == null)
                    return NotFound(new { message = "Aucune fiche de paie trouvée pour cette période." });

                if (payrollResult.Status != PayrollResultStatus.OK)
                    return BadRequest(new { message = $"La fiche de paie est en erreur : {payrollResult.ErrorMessage}" });

                if (leaveBalance == null)
                {
                    // FIX: Guard null employee before accessing CompanyId
                    if (employee?.CompanyId == null)
                        return StatusCode(500, new { message = "Impossible de déterminer la société de l'employé." });

                    var recalcResult = await _leaveBalanceService.RecalculateAsync(employee.CompanyId, employeeId, 2, asOfDate, 0);
                    leaveBalance = recalcResult?.Balance;
                    if (leaveBalance == null)
                        return StatusCode(500, new { message = "Impossible de recalculer le solde de congé." });
                }

                // FIX: null-conditional on employee?.FirstName
                _logger.LogInformation(
                    "BalanceId {BalanceId} de l'employee {EmployeeName} : CarryIn={CarryIn} CarryOut={CarryOut} Used={Used}",
                    leaveBalance.Id, employee?.FirstName,
                    leaveBalance.CarryInDays, leaveBalance.CarryOutDays, leaveBalance.UsedDays);

                var pdfBytes = GeneratePdf(payrollResult, leaveBalance);
                var fileName = $"Fiche_Paie_{payrollResult.Employee.FirstName}_{payrollResult.Employee.LastName}_{month:D2}_{year}.pdf";

                _logger.LogInformation("📄 Fiche de paie PDF générée pour {Employee} - {Month}/{Year}",
                    $"{payrollResult.Employee.FirstName} {payrollResult.Employee.LastName}", month, year);

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du PDF pour employé {EmployeeId}, {Month}/{Year}",
                    employeeId, month, year);
                return StatusCode(500, new { message = "Erreur lors de la génération du PDF", error = ex.Message });
            }
        }

        private byte[] GeneratePdf(PayrollResult payroll, LeaveBalance leaveBalance)
        {
            var html = BuildPayslipHtml(payroll, leaveBalance);

            var renderer = new ChromePdfRenderer();
            renderer.RenderingOptions.PaperSize = IronPdf.Rendering.PdfPaperSize.A4;
            renderer.RenderingOptions.MarginTop    = 15;
            renderer.RenderingOptions.MarginBottom = 15;
            renderer.RenderingOptions.MarginLeft   = 15;
            renderer.RenderingOptions.MarginRight  = 15;
            renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print;

            var document = renderer.RenderHtmlAsPdf(html);
            return document.BinaryData;
        }

        // =====================================================================
        // BUILDER HTML complet de la fiche de paie
        // =====================================================================
        private string BuildPayslipHtml(PayrollResult payroll, LeaveBalance leaveBalance)
        {
            var contract  = payroll.Employee.Contracts?.FirstOrDefault();
            var sb        = new StringBuilder();

            // ── Ancienneté ────────────────────────────────────────────────────
            string anciennete = "N/A";
            if (contract?.StartDate != null)
            {
                var periodEnd = new DateTime(payroll.Year, payroll.Month,
                    DateTime.DaysInMonth(payroll.Year, payroll.Month));
                var sen   = periodEnd - contract.StartDate;
                int yrs   = (int)(sen.TotalDays / 365.25);
                int mths  = (int)((sen.TotalDays % 365.25) / 30.44);
                anciennete = $"{yrs} ans {mths} mois";
            }

            // ── Primes / indemnités ───────────────────────────────────────────
            var primesImposables = payroll.Primes?.Where(p => p.IsTaxable).OrderBy(p => p.Ordre).ToList()  ?? new();
            var indemnites       = payroll.Primes?.Where(p => !p.IsTaxable).OrderBy(p => p.Ordre).ToList() ?? new();

            // ── Taux IR ───────────────────────────────────────────────────────
            string irRate = "";
            if ((payroll.IrTaux ?? 0) > 0)
                irRate = $"{payroll.IrTaux!.Value * 100m:0.##}%";
            else if ((payroll.NetImposable ?? 0) > 0 && (payroll.ImpotRevenu ?? 0) > 0)
                irRate = $"{((payroll.ImpotRevenu ?? 0) / payroll.NetImposable!.Value * 100m):0.##}%";

            // ── Taux CIMR ─────────────────────────────────────────────────────
            string cimrRate = payroll.Employee.CimrEmployeeRate.HasValue
                ? $"{payroll.Employee.CimrEmployeeRate:0.##}%"
                : "";
            string mutRate = payroll.Employee.PrivateInsuranceRate.HasValue
                ? $"{payroll.Employee.PrivateInsuranceRate:0.##}%"
                : "";

            decimal totalGains = (payroll.TotalBrut ?? 0) + (payroll.TotalIndemnites ?? 0);
            decimal leaveAvailable = leaveBalance.CarryInDays + leaveBalance.AccruedDays - leaveBalance.UsedDays;

            // ─────────────────────────────────────────────────────────────────
            // HTML
            // ─────────────────────────────────────────────────────────────────
            sb.Append(@"<!DOCTYPE html>
<html lang='fr'>
<head>
<meta charset='UTF-8'/>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: Arial, sans-serif; font-size: 8.5pt; color: #222; }
  h1   { font-size: 13pt; }

  /* ── Layout ── */
  .header-row   { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 6px; }
  .header-left  { flex: 2; }
  .header-right { flex: 1; text-align: right; }
  .header-right .title { font-size: 15pt; font-weight: bold; }
  .header-right .period{ font-size: 10pt; font-weight: bold; margin-top: 4px; }
  hr.sep { border: none; border-top: 1px solid #bbb; margin: 8px 0; }

  /* ── Employé ── */
  .emp-box { background: #f0f0f0; padding: 10px; margin-bottom: 8px; }
  .emp-row { display: flex; gap: 6px; margin-bottom: 4px; }
  .emp-row > div { flex: 1; }

  /* ── Tableau principal ── */
  table { width: 100%; border-collapse: collapse; margin-bottom: 8px; font-size: 8pt; }
  table th { background: #1a3c6e; color: #fff; padding: 5px 4px; text-align: right; font-size: 8.5pt; }
  table th.label-col { text-align: left; }
  table td { padding: 3px 4px; border-bottom: 0.5px solid #ddd; }
  table td.label-col { text-align: left; }
  table td.right-col { text-align: right; }
  table tr.bold td { font-weight: bold; }
  table tr.sep-row td { padding: 1px 0; border-bottom: 1.5px solid #999; }
  table tr.net-row td { background: #c8e6c9; font-weight: bold; font-size: 11pt; }
  table tr.net-label { background: #d5efd7; font-weight: bold; font-size: 11pt; }

  /* ── Summary band ── */
  .summary-table th { font-size: 7.5pt; background: #555; }
  .summary-table td { text-align: center; font-size: 8pt; }

  /* ── Congés ── */
  .conge-box { background: #e8f0fe; padding: 8px; margin-top: 8px; }
  .conge-box .conge-title { font-weight: bold; font-size: 9pt; margin-bottom: 6px; }
  .conge-row { display: flex; gap: 8px; }
  .conge-row > div { flex: 1; }

  /* ── Footer ── */
  .footer { text-align: center; font-size: 7pt; color: #777; margin-top: 10px; border-top: 0.5px solid #ddd; padding-top: 4px; }
</style>
</head>
<body>
");
            // ── HEADER ──────────────────────────────────────────────────────
            // Récupérer le logo de l'entreprise (type "logo")
            var companyLogo = payroll.Employee.Company.Documents?
                .FirstOrDefault(d => d.DocumentType == "logo" && d.DeletedAt == null);

            string logoHtml = "";
            if (companyLogo != null && !string.IsNullOrWhiteSpace(companyLogo.FilePath))
            {
                // Convertir le chemin relatif en chemin absolu ou en base64
                var logoPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, companyLogo.FilePath);

                if (System.IO.File.Exists(logoPath))
                {
                    try
                    {
                        var logoBytes = System.IO.File.ReadAllBytes(logoPath);
                        var logoBase64 = Convert.ToBase64String(logoBytes);
                        var extension = Path.GetExtension(companyLogo.FilePath).ToLowerInvariant();
                        var mimeType = extension switch
                        {
                            ".png" => "image/png",
                            ".jpg" or ".jpeg" => "image/jpeg",
                            ".gif" => "image/gif",
                            _ => "image/png"
                        };
                        logoHtml = $"<img src='data:{mimeType};base64,{logoBase64}' alt='Logo' style='max-height: 80px; max-width: 150px; margin-bottom: 8px;' />";
                        _logger.LogWarning($"Logo Generated {logoHtml}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Impossible de charger le logo pour la fiche de paie");
                    }
                }
            }

            sb.Append($@"
<div class='header-row'>
  <div class='header-left'>
    {logoHtml}
    <h1>{H(payroll.Employee.Company.CompanyName)}</h1>
    <div>ICE N° : {H(payroll.Employee.Company.IceNumber ?? "N/A")}</div>
    <div>CNSS N° : {H(payroll.Employee.Company.CnssNumber ?? "N/A")}</div>
    <div>IF N° : {H(payroll.Employee.Company.IfNumber ?? "N/A")}</div>
    <div>Adresse : {H(payroll.Employee.Company.CompanyAddress ?? "N/A")}</div>
    <div><b>{H(payroll.Employee.Company.City?.CityName ?? "N/A")}</b></div>
  </div>
  <div class='header-right'>
    <div class='title'>BULLETIN DE PAIE</div>
    <div class='period'>Période : {GetMonthName(payroll.Month)} {payroll.Year}</div>
  </div>
</div>
<hr class='sep'/>
");
            // ── INFOS EMPLOYÉ ────────────────────────────────────────────────
            string dateEmbauche = contract?.StartDate != null
                ? contract.StartDate.ToString("dd/MM/yyyy") : "N/A";
            string cimrNum    = payroll.Employee.CimrNumber != null ? $"CIMR : {H(payroll.Employee.CimrNumber)}" : "";
            string mutuelleNum= payroll.Employee.PrivateInsuranceNumber != null
                ? $"Mutuelle : {H(payroll.Employee.PrivateInsuranceNumber)}" : "";
            string periodLabel= $"01/{payroll.Month:D2}/{payroll.Year} - {DateTime.DaysInMonth(payroll.Year, payroll.Month):D2}/{payroll.Month:D2}/{payroll.Year}";

            sb.Append($@"
<div class='emp-box'>
  <div class='emp-row'>
    <div><b>Nom : {H(payroll.Employee.FirstName)} {H(payroll.Employee.LastName)}</b></div>
    <div>CIN : {H(payroll.Employee.CinNumber ?? "N/A")}</div>
    <div>CNSS : {H(payroll.Employee.CnssNumber ?? "N/A")}</div>
    <div>{cimrNum}</div>
  </div>
  <div class='emp-row'>
    <div>{mutuelleNum}</div>
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
</div>
");
            // ── TABLEAU PRINCIPAL ─────────────────────────────────────────────
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
            // Salaire de base
            TR(sb, "Salaire de base", "", "", F(payroll.SalaireBase), "");
            if ((payroll.HeuresSupp25  ?? 0) > 0) TR(sb, "Heures supplémentaires 25%",  "", "25%",  F(payroll.HeuresSupp25),  "");
            if ((payroll.HeuresSupp50  ?? 0) > 0) TR(sb, "Heures supplémentaires 50%",  "", "50%",  F(payroll.HeuresSupp50),  "");
            if ((payroll.HeuresSupp100 ?? 0) > 0) TR(sb, "Heures supplémentaires 100%", "", "100%", F(payroll.HeuresSupp100), "");
            if ((payroll.Conges        ?? 0) > 0) TR(sb, "Congés payés",       "", "", F(payroll.Conges),       "");
            if ((payroll.JoursFeries   ?? 0) > 0) TR(sb, "Jours fériés",        "", "", F(payroll.JoursFeries),  "");
            if ((payroll.PrimeAnciennete ?? 0) > 0) TR(sb, "Prime d'ancienneté",  "", "", F(payroll.PrimeAnciennete), "");

            foreach (var p in primesImposables) TR(sb, H(p.Label), "", "", p.Montant.ToString("N2"), "");
            if (!primesImposables.Any())
            {
                if ((payroll.PrimeImposable1 ?? 0) > 0) TR(sb, "Prime imposable 1", "", "", F(payroll.PrimeImposable1), "");
                if ((payroll.PrimeImposable2 ?? 0) > 0) TR(sb, "Prime imposable 2", "", "", F(payroll.PrimeImposable2), "");
                if ((payroll.PrimeImposable3 ?? 0) > 0) TR(sb, "Prime imposable 3", "", "", F(payroll.PrimeImposable3), "");
            }
            if ((payroll.TotalNiExcedentImposable  ?? 0) > 0) TR(sb, "Excédent indemnités (imposable)",    "", "", F(payroll.TotalNiExcedentImposable), "");

            TRSep(sb);
            TR(sb, "SALAIRE BRUT IMPOSABLE", "", "", F(payroll.TotalBrut) ?? "0.00", "", bold: true);
            TRSep(sb);

            if ((payroll.FraisProfessionnels ?? 0) > 0)
                TR(sb, "Frais professionnels", F(payroll.BrutImposable), "25%", "", F(payroll.FraisProfessionnels));

            // Indemnités NI dynamiques
            foreach (var ind in indemnites) TR(sb, $"{H(ind.Label)} (NI)", "", "", ind.Montant.ToString("N2"), "");

            // Indemnités NI scalaires
            if ((payroll.IndemniteRepresentation   ?? 0) > 0) TR(sb, "Indemnité de représentation (NI)", "", "", F(payroll.IndemniteRepresentation), "");
            if ((payroll.PrimeTransport            ?? 0) > 0) TR(sb, "Prime de transport (NI)",          "", "", F(payroll.PrimeTransport), "");
            if ((payroll.PrimePanier               ?? 0) > 0) TR(sb, "Prime de panier (NI)",             "", "", F(payroll.PrimePanier), "");
            if ((payroll.IndemniteDeplacement      ?? 0) > 0) TR(sb, "Indemnité de déplacement (NI)",    "", "", F(payroll.IndemniteDeplacement), "");
            if ((payroll.IndemniteCaisse           ?? 0) > 0) TR(sb, "Indemnité de caisse (NI)",         "", "", F(payroll.IndemniteCaisse), "");
            if ((payroll.PrimeSalissure            ?? 0) > 0) TR(sb, "Prime de salissure (NI)",          "", "", F(payroll.PrimeSalissure), "");
            if ((payroll.GratificationsFamilial    ?? 0) > 0) TR(sb, "Gratifications familiales (NI)",   "", "", F(payroll.GratificationsFamilial), "");
            if ((payroll.PrimeVoyageMecque         ?? 0) > 0) TR(sb, "Prime de voyage à la Mecque (NI)", "", "", F(payroll.PrimeVoyageMecque), "");
            if ((payroll.IndemniteLicenciement     ?? 0) > 0) TR(sb, "Indemnité de licenciement (NI)",   "", "", F(payroll.IndemniteLicenciement), "");
            if ((payroll.IndemniteKilometrique     ?? 0) > 0) TR(sb, "Indemnité kilométrique (NI)",      "", "", F(payroll.IndemniteKilometrique), "");
            if ((payroll.PrimeTourne               ?? 0) > 0) TR(sb, "Prime de tournée (NI)",            "", "", F(payroll.PrimeTourne), "");
            if ((payroll.PrimeOutillage            ?? 0) > 0) TR(sb, "Prime d'outillage (NI)",           "", "", F(payroll.PrimeOutillage), "");
            if ((payroll.AideMedicale              ?? 0) > 0) TR(sb, "Aide médicale (NI)",               "", "", F(payroll.AideMedicale), "");
            if ((payroll.AutresPrimesNonImposable  ?? 0) > 0) TR(sb, "Autres primes non imposables (NI)","", "", F(payroll.AutresPrimesNonImposable), "");

            TRSep(sb);

            // Cotisations salariales
            if ((payroll.CnssPartSalariale     ?? 0) > 0) TR(sb, "CNSS (part salariale)",     F(payroll.CnssBase     ?? payroll.BrutImposable), "4.48%",   "", F(payroll.CnssPartSalariale));
            if ((payroll.CimrPartSalariale     ?? 0) > 0) TR(sb, "CIMR (part salariale)",     F(payroll.CimrBase     ?? payroll.BrutImposable), cimrRate,  "", F(payroll.CimrPartSalariale));
            if ((payroll.AmoPartSalariale      ?? 0) > 0) TR(sb, "AMO (part salariale)",      F(payroll.AmoBase      ?? payroll.BrutImposable), "2.26%",   "", F(payroll.AmoPartSalariale));
            if ((payroll.MutuellePartSalariale ?? 0) > 0) TR(sb, "Mutuelle (part salariale)", F(payroll.MutuelleBase ?? payroll.BrutImposable), mutRate,   "", F(payroll.MutuellePartSalariale));
            if ((payroll.ImpotRevenu           ?? 0) > 0) TR(sb, "Impôt sur le revenu (IR)",  F(payroll.NetImposable),                          irRate,    "", F(payroll.ImpotRevenu));
            if ((payroll.Arrondi               ?? 0) != 0) TR(sb, "Arrondi",                  "", "", "", F(payroll.Arrondi));
            if ((payroll.AvanceSurSalaire      ?? 0) > 0) TR(sb, "Avance sur salaire",        "", "", "", F(payroll.AvanceSurSalaire));
            if ((payroll.InteretSurLogement    ?? 0) > 0) TR(sb, "Intérêt sur logement",      "", "", "", F(payroll.InteretSurLogement));

            TRSep(sb);

            TR(sb, "TOTAL GAINS",    "", "", totalGains.ToString("N2"),              "",                          bold: true);
            TR(sb, "TOTAL RETENUES", "", "", "",                                     F(payroll.TotalRetenues) ?? "0.00", bold: true);
            TRSep(sb);

            // NET À PAYER
            sb.Append($@"
    <tr>
      <td colspan='4' class='label-col' style='background:#c8e6c9;font-weight:bold;font-size:11pt;padding:6px'>NET À PAYER</td>
      <td class='right-col'  style='background:#b9dfbb;font-weight:bold;font-size:11pt;padding:6px'>{F(payroll.NetAPayer)} MAD</td>
    </tr>
  </tbody>
</table>
");
            // ── SUMMARY BAND ─────────────────────────────────────────────────
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
      <td>{F(payroll.AmoPartPatronale)  ?? "0.00"}</td>
      <td>{F(payroll.CimrPartPatronale) ?? "0.00"}</td>
      <td>{F(payroll.MutuellePartPatronale) ?? "0.00"}</td>
      <td>{F(payroll.BrutImposable) ?? "0.00"}</td>
      <td>{F(payroll.NetImposable)  ?? "0.00"}</td>
    </tr>
  </tbody>
  <thead>
    <tr>
      <th>IR</th><th>Total Cotis. Sal.</th><th>Total Cotis. Pat.</th><th>Total Retenues</th><th>Avance Salaire</th><th>Intérêt Logement</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>{F(payroll.ImpotRevenu)              ?? "0.00"}</td>
      <td>{F(payroll.TotalCotisationsSalariales)  ?? "0.00"}</td>
      <td>{F(payroll.TotalCotisationsPatronales) ?? "0.00"}</td>
      <td>{F(payroll.TotalRetenues)            ?? "0.00"}</td>
      <td>{F(payroll.AvanceSurSalaire)         ?? "0.00"}</td>
      <td>{F(payroll.InteretSurLogement)       ?? "0.00"}</td>
    </tr>
  </tbody>
</table>
");
            // ── SOLDE DE CONGÉS ───────────────────────────────────────────────
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

            // ── Signature ───────────────────────────────────────────────────────
            sb.Append($@"
<div style='margin-top: 20px; display: flex; justify-content: space-between; align-items: flex-start;'>
  <div style='flex: 1; text-align: left;'>
    <div style='font-size: 9pt; margin-bottom: 4px;'>Fait à <b>{H(payroll.Employee.Company.City?.CityName ?? "N/A")}</b></div>
    <div style='font-size: 9pt; margin-bottom: 30px;'>Le <b>{DateTime.Now:dd/MM/yyyy}</b></div>
    <div style='font-size: 9pt; font-weight: bold; margin-bottom: 4px;'>Signature de l'employeur</div>
    <div style='font-size: 8pt; color: #555; margin-top: 40px; border-top: 1px solid #999; padding-top: 4px; max-width: 200px;'>{H(payroll.Company.SignatoryName ?? "N/A")}</div>
  </div>
</div>
</body>
</html>
");

            return sb.ToString();
        }

        // ── HTML helpers ─────────────────────────────────────────────────────
        private static string H(string? s) =>
            System.Net.WebUtility.HtmlEncode(s ?? "");

        private static void TR(StringBuilder sb, string label, string? baseVal, string? rate,
                                string? gain, string? retenue, bool bold = false)
        {
            var b = bold ? " font-weight:bold;" : "";
            sb.Append($@"
    <tr>
      <td class='label-col'  style='{b}'>{label}</td>
      <td class='right-col'  style='{b}'>{baseVal  ?? ""}</td>
      <td class='right-col'  style='{b}'>{rate     ?? ""}</td>
      <td class='right-col'  style='{b}'>{gain     ?? ""}</td>
      <td class='right-col'  style='{b}'>{retenue  ?? ""}</td>
    </tr>");
        }

        private static void TRSep(StringBuilder sb)
        {
            sb.Append(@"
    <tr class='sep-row'><td colspan='5' style='padding:1px 0;border-bottom:1.5px solid #999'></td></tr>");
        }

        /// <summary>Formats a nullable decimal as "N2".</summary>
        private static string? F(decimal? value) => value?.ToString("N2");

        private static string GetMonthName(int month) => month switch
        {
            1  => "Janvier",  2  => "Février",   3  => "Mars",
            4  => "Avril",    5  => "Mai",        6  => "Juin",
            7  => "Juillet",  8  => "Août",       9  => "Septembre",
            10 => "Octobre",  11 => "Novembre",   12 => "Décembre",
            _  => "Inconnu"
        };
    }
}
/*

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                });
            });

            return document.GeneratePdf();

            // =====================================================================
            // HEADER — Company info (left) + Title/Period (right)
            // =====================================================================
            void ComposeHeader(IContainer container)
            {
                container.Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        // Left block: company details
                        // AppDbContext confirms: CompanyName, IceNumber, CnssNumber, IfNumber,
                        //                        CompanyAddress, City.CityName
                        row.RelativeItem(2).Column(c =>
                        {
                            c.Item().Text(payroll.Employee.Company.CompanyName).FontSize(13).Bold();
                            c.Item().Text($"ICE N° : {payroll.Employee.Company.IceNumber ?? "N/A"}").FontSize(8);
                            c.Item().Text($"CNSS N° : {payroll.Employee.Company.CnssNumber ?? "N/A"}").FontSize(8);
                            c.Item().Text($"IF N° : {payroll.Employee.Company.IfNumber ?? "N/A"}").FontSize(8);
                            c.Item().Text($"Adresse : {payroll.Employee.Company.CompanyAddress ?? "N/A"}").FontSize(8);
                            c.Item().Text(payroll.Employee.Company.City?.CityName ?? "N/A").FontSize(8).Bold();
                        });

                        // Right block: bulletin title + period
                        row.RelativeItem(1).AlignRight().Column(c =>
                        {
                            c.Item().Text("BULLETIN DE PAIE").FontSize(15).Bold();
                            c.Item().Text($"Période : {GetMonthName(payroll.Month)} {payroll.Year}").FontSize(10).Bold();
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });
            }

            // =====================================================================
            // CONTENT
            // =====================================================================
            void ComposeContent(IContainer container)
            {
                container.Column(col =>
                {
                    col.Item().Element(ComposeEmployeeInfo);
                    col.Item().PaddingVertical(6);
                    col.Item().Element(ComposePayrollTable);
                    col.Item().PaddingVertical(6);
                    col.Item().Element(ComposeSummaryBand);
                    col.Item().PaddingVertical(6);
                    col.Item().Element(ComposeLeaveBalance);
                });
            }

            // =====================================================================
            // EMPLOYEE INFO — uses only properties confirmed in AppDbContext/Employee config
            //   FirstName, LastName, CinNumber, CnssNumber, CimrNumber,
            //   PrivateInsuranceNumber, DateOfBirth,
            //   Departement.DepartementName, Contracts[0].JobPosition.Name,
            //   Contracts[0].StartDate
            //   MaritalStatus.NameFr (via navigation — confirmed in DB config)
            // =====================================================================
            void ComposeEmployeeInfo(IContainer container)
            {
                var contract = payroll.Employee.Contracts?.FirstOrDefault();

                container.Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
                {
                    // Row 1: Name | CIN | CNSS | CIMR
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Nom : {payroll.Employee.FirstName} {payroll.Employee.LastName}").Bold();
                        row.RelativeItem().Text($"CIN : {payroll.Employee.CinNumber ?? "N/A"}");
                        row.RelativeItem().Text($"CNSS : {payroll.Employee.CnssNumber ?? "N/A"}");
                        row.RelativeItem().Text(payroll.Employee.CimrNumber != null
                            ? $"CIMR : {payroll.Employee.CimrNumber}" : "");
                    });

                    col.Item().PaddingVertical(3);

                    // Row 2: Mutuelle | Département | Fonction | Situation familiale (via MaritalStatus nav)
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(payroll.Employee.PrivateInsuranceNumber != null
                            ? $"Mutuelle : {payroll.Employee.PrivateInsuranceNumber}" : "");
                        row.RelativeItem().Text($"Département : {payroll.Employee.Departement?.DepartementName ?? "N/A"}");
                        row.RelativeItem().Text($"Fonction : {contract?.JobPosition?.Name ?? "N/A"}");
                        // MaritalStatus navigation confirmed in OnModelCreating
                        row.RelativeItem().Text($"Situation fam. : {payroll.Employee.MaritalStatus?.NameFr ?? "N/A"}");
                    });

                    col.Item().PaddingVertical(3);

                    // Row 3: Date d'embauche | Ancienneté (FIX: relative to payroll period) | Date naissance
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Date d'embauche : {(contract?.StartDate != null ? contract.StartDate.ToString("dd/MM/yyyy") : "N/A")}");

                        if (contract?.StartDate != null)
                        {
                            // FIX: seniority relative to payroll period end, not DateTime.Now
                            var periodEnd = new DateTime(payroll.Year, payroll.Month, DateTime.DaysInMonth(payroll.Year, payroll.Month));
                            var seniority = periodEnd - contract.StartDate;
                            int yrs = (int)(seniority.TotalDays / 365.25);
                            int mths = (int)((seniority.TotalDays % 365.25) / 30.44);
                            row.RelativeItem().Text($"Ancienneté : {yrs} ans {mths} mois");
                        }
                        else
                            row.RelativeItem().Text("Ancienneté : N/A");

                        // DateOfBirth confirmed: entity.Property(e => e.DateOfBirth).IsRequired()
                        row.RelativeItem().Text($"Date naissance : {payroll.Employee.DateOfBirth:dd/MM/yyyy}");
                        row.RelativeItem().Text($"Période : 01/{payroll.Month:D2}/{payroll.Year} - {DateTime.DaysInMonth(payroll.Year, payroll.Month):D2}/{payroll.Month:D2}/{payroll.Year}").Bold();
                    });
                });
            }

            // =====================================================================
            // MAIN PAYROLL TABLE
            // All columns referenced below exist in the PayrollResults DB table
            // per the schema provided: SalaireBase, HeuresSupp25/50/100, Conges,
            // JoursFeries, PrimeAnciennete, PrimeImposable1/2/3, TotalBrut,
            // FraisProfessionnels, BrutImposable, IndemniteRepresentation,
            // PrimeTransport, PrimePanier, IndemniteDeplacement, IndemniteCaisse,
            // PrimeSalissure, GratificationsFamilial, PrimeVoyageMecque,
            // IndemniteLicenciement, IndemniteKilometrique, PrimeTourne,
            // PrimeOutillage, AideMedicale, AutresPrimesNonImposable,
            // CnssPartSalariale, CimrPartSalariale, AmoPartSalariale,
            // MutuellePartSalariale, ImpotRevenu, Arrondi, AvanceSurSalaire,
            // InteretSurLogement, TotalBrut, TotalIndemnites, TotalRetenues, NetAPayer
            // =====================================================================
            void ComposePayrollTable(IContainer container)
            {
                container.Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3.5f); // Libellé
                        cols.RelativeColumn(1.2f); // Base
                        cols.RelativeColumn(0.8f); // Taux
                        cols.RelativeColumn(1.2f); // Gain
                        cols.RelativeColumn(1.2f); // Retenue
                    });

                    table.Header(header =>
                    {
                        void H(string t) => header.Cell()
                            .Background(Colors.Blue.Darken2).Padding(5)
                            .Text(t).FontColor(Colors.White).Bold().FontSize(9).AlignRight();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                            .Text("LIBELLÉ").FontColor(Colors.White).Bold().FontSize(9);
                        H("BASE"); H("TAUX"); H("GAIN"); H("RETENUE");
                    });

                    // ── RÉMUNÉRATION DE BASE ────────────────────────────────────────
                    AddRow(table, "Salaire de base", "", "", F(payroll.SalaireBase), "");

                    if ((payroll.HeuresSupp25 ?? 0) > 0)
                        AddRow(table, "Heures supplémentaires 25%", "", "25%", F(payroll.HeuresSupp25), "");
                    if ((payroll.HeuresSupp50 ?? 0) > 0)
                        AddRow(table, "Heures supplémentaires 50%", "", "50%", F(payroll.HeuresSupp50), "");
                    if ((payroll.HeuresSupp100 ?? 0) > 0)
                        AddRow(table, "Heures supplémentaires 100%", "", "100%", F(payroll.HeuresSupp100), "");
                    if ((payroll.Conges ?? 0) > 0)
                        AddRow(table, "Congés payés", "", "", F(payroll.Conges), "");
                    if ((payroll.JoursFeries ?? 0) > 0)
                        AddRow(table, "Jours fériés", "", "", F(payroll.JoursFeries), "");
                    if ((payroll.PrimeAnciennete ?? 0) > 0)
                        AddRow(table, "Prime d'ancienneté", "", "", F(payroll.PrimeAnciennete), "");

                    // Dynamic taxable primes (PayrollResultPrimes navigation — confirmed in DB config)
                    var primesImposables = payroll.Primes?
                        .Where(p => p.IsTaxable).OrderBy(p => p.Ordre).ToList() ?? new();
                    foreach (var p in primesImposables)
                        AddRow(table, p.Label, "", "", p.Montant.ToString("N2"), "");

                    // Fallback to scalar columns when Primes collection is empty
                    if (!primesImposables.Any())
                    {
                        if ((payroll.PrimeImposable1 ?? 0) > 0)
                            AddRow(table, "Prime imposable 1", "", "", F(payroll.PrimeImposable1), "");
                        if ((payroll.PrimeImposable2 ?? 0) > 0)
                            AddRow(table, "Prime imposable 2", "", "", F(payroll.PrimeImposable2), "");
                        if ((payroll.PrimeImposable3 ?? 0) > 0)
                            AddRow(table, "Prime imposable 3", "", "", F(payroll.PrimeImposable3), "");
                    }

                    AddSeparatorRow(table);
                    AddRow(table, "SALAIRE BRUT IMPOSABLE", "", "", F(payroll.TotalBrut) ?? "0.00", "", isBold: true);
                    AddSeparatorRow(table);

                    // Frais professionnels (BrutImposable confirmed in DB schema provided)
                    if ((payroll.FraisProfessionnels ?? 0) > 0)
                        AddRow(table, "Frais professionnels", F(payroll.BrutImposable), "25%", "", F(payroll.FraisProfessionnels));

                    // ── INDEMNITÉS NON IMPOSABLES ───────────────────────────────────
                    var indemnites = payroll.Primes?
                        .Where(p => !p.IsTaxable).OrderBy(p => p.Ordre).ToList() ?? new();
                    foreach (var ind in indemnites)
                        AddRow(table, $"{ind.Label} (NI)", "", "", ind.Montant.ToString("N2"), "");

                    // All scalar NI columns from DB schema
                    if ((payroll.IndemniteRepresentation ?? 0) > 0)
                        AddRow(table, "Indemnité de représentation (NI)", "", "", F(payroll.IndemniteRepresentation), "");
                    if ((payroll.PrimeTransport ?? 0) > 0)
                        AddRow(table, "Prime de transport (NI)", "", "", F(payroll.PrimeTransport), "");
                    if ((payroll.PrimePanier ?? 0) > 0)
                        AddRow(table, "Prime de panier (NI)", "", "", F(payroll.PrimePanier), "");
                    if ((payroll.IndemniteDeplacement ?? 0) > 0)
                        AddRow(table, "Indemnité de déplacement (NI)", "", "", F(payroll.IndemniteDeplacement), "");
                    if ((payroll.IndemniteCaisse ?? 0) > 0)
                        AddRow(table, "Indemnité de caisse (NI)", "", "", F(payroll.IndemniteCaisse), "");
                    if ((payroll.PrimeSalissure ?? 0) > 0)
                        AddRow(table, "Prime de salissure (NI)", "", "", F(payroll.PrimeSalissure), "");
                    if ((payroll.GratificationsFamilial ?? 0) > 0)
                        AddRow(table, "Gratifications familiales (NI)", "", "", F(payroll.GratificationsFamilial), "");
                    if ((payroll.PrimeVoyageMecque ?? 0) > 0)
                        AddRow(table, "Prime de voyage à la Mecque (NI)", "", "", F(payroll.PrimeVoyageMecque), "");
                    if ((payroll.IndemniteLicenciement ?? 0) > 0)
                        AddRow(table, "Indemnité de licenciement (NI)", "", "", F(payroll.IndemniteLicenciement), "");
                    if ((payroll.IndemniteKilometrique ?? 0) > 0)
                        AddRow(table, "Indemnité kilométrique (NI)", "", "", F(payroll.IndemniteKilometrique), "");
                    if ((payroll.PrimeTourne ?? 0) > 0)
                        AddRow(table, "Prime de tournée (NI)", "", "", F(payroll.PrimeTourne), "");
                    if ((payroll.PrimeOutillage ?? 0) > 0)
                        AddRow(table, "Prime d'outillage (NI)", "", "", F(payroll.PrimeOutillage), "");
                    if ((payroll.AideMedicale ?? 0) > 0)
                        AddRow(table, "Aide médicale (NI)", "", "", F(payroll.AideMedicale), "");
                    if ((payroll.AutresPrimesNonImposable ?? 0) > 0)
                        AddRow(table, "Autres primes non imposables (NI)", "", "", F(payroll.AutresPrimesNonImposable), "");
                    if ((payroll.TotalNiExcedentImposable ?? 0) > 0)
                        AddRow(table, "Excédent indemnités (imposable)", "", "", F(payroll.TotalNiExcedentImposable), "");

                    AddSeparatorRow(table);

                    // ── COTISATIONS SALARIALES ──────────────────────────────────────
                    // BrutImposable used as base — confirmed in DB schema
                    // Employee.CimrEmployeeRate confirmed: entity.Property(e => e.CimrEmployeeRate)
                    if ((payroll.CnssPartSalariale ?? 0) > 0)
                        AddRow(table, "CNSS (part salariale)", F(payroll.CnssBase ?? payroll.BrutImposable), "4.48%", "", F(payroll.CnssPartSalariale));
                    if ((payroll.CimrPartSalariale ?? 0) > 0)
                    {
                        var cimrRate = payroll.Employee.CimrEmployeeRate.HasValue
                            ? $"{payroll.Employee.CimrEmployeeRate:0.##}%"
                            : "";
                        AddRow(table, "CIMR (part salariale)", F(payroll.CimrBase ?? payroll.BrutImposable), cimrRate, "", F(payroll.CimrPartSalariale));
                    }
                    if ((payroll.AmoPartSalariale ?? 0) > 0)
                        AddRow(table, "AMO (part salariale)", F(payroll.AmoBase ?? payroll.BrutImposable), "2.26%", "", F(payroll.AmoPartSalariale));
                    if ((payroll.MutuellePartSalariale ?? 0) > 0)
                    {
                        // Employee.PrivateInsuranceRate confirmed in DB config
                        var mutRate = payroll.Employee.PrivateInsuranceRate.HasValue
                            ? $"{payroll.Employee.PrivateInsuranceRate:0.##}%"
                            : "";
                        AddRow(table, "Mutuelle (part salariale)", F(payroll.MutuelleBase ?? payroll.BrutImposable), mutRate, "", F(payroll.MutuellePartSalariale));
                    }
                    if ((payroll.ImpotRevenu ?? 0) > 0)
                    {
                        var irBase = F(payroll.NetImposable);
                        // Taux barème stocké (ex: 0.34 → "34%"), fallback sur taux effectif
                        string irRate;
                        if ((payroll.IrTaux ?? 0) > 0)
                            irRate = $"{payroll.IrTaux!.Value * 100m:0.##}%";
                        else if ((payroll.NetImposable ?? 0) > 0)
                            irRate = $"{((payroll.ImpotRevenu ?? 0) / payroll.NetImposable!.Value * 100m):0.##}%";
                        else
                            irRate = "";
                        AddRow(table, "Impôt sur le revenu (IR)", irBase, irRate, "", F(payroll.ImpotRevenu));
                    }
                    if ((payroll.Arrondi ?? 0) != 0)
                        AddRow(table, "Arrondi", "", "", "", F(payroll.Arrondi));
                    if ((payroll.AvanceSurSalaire ?? 0) > 0)
                        AddRow(table, "Avance sur salaire", "", "", "", F(payroll.AvanceSurSalaire));
                    if ((payroll.InteretSurLogement ?? 0) > 0)
                        AddRow(table, "Intérêt sur logement", "", "", "", F(payroll.InteretSurLogement));

                    AddSeparatorRow(table);

                    // ── TOTAUX ──────────────────────────────────────────────────────
                    // FIX: parentheses around each operand — avoids operator precedence bug
                    decimal totalGains = (payroll.TotalBrut ?? 0) + (payroll.TotalIndemnites ?? 0);
                    AddRow(table, "TOTAL GAINS", "", "", totalGains.ToString("N2"), "", isBold: true);
                    AddRow(table, "TOTAL RETENUES", "", "", "", F(payroll.TotalRetenues) ?? "0.00", isBold: true);

                    AddSeparatorRow(table);

                    // NET À PAYER
                    table.Cell().ColumnSpan(4).Background(Colors.Green.Lighten3).Padding(6)
                        .Text("NET À PAYER").Bold().FontSize(11);
                    table.Cell().Background(Colors.Green.Lighten2).Padding(6)
                        .Text($"{F(payroll.NetAPayer)} MAD").Bold().FontSize(11).AlignRight();
                });
            }

            // =====================================================================
            // SUMMARY BAND — Patronal contributions + Net/Brut imposable
            // All columns confirmed in DB schema:
            //   CnssPartPatronale, AmoPartPatronale, CimrPartPatronale,
            //   MutuellePartPatronale, BrutImposable, NetImposable,
            //   ImpotRevenu, TotalCotisationsSalariales, TotalCotisationsPatronales,
            //   TotalRetenues, AvanceSurSalaire, InteretSurLogement
            // =====================================================================
            void ComposeSummaryBand(IContainer container)
            {
                container.Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(); cols.RelativeColumn();
                        cols.RelativeColumn(); cols.RelativeColumn();
                        cols.RelativeColumn(); cols.RelativeColumn();
                    });

                    void SH(string t) => table.Cell()
                        .Background(Colors.Grey.Darken1).Padding(4)
                        .Text(t).FontColor(Colors.White).Bold().FontSize(7.5f).AlignCenter();

                    void SV(string t) => table.Cell()
                        .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                        .Padding(4).Text(t).AlignCenter().FontSize(8);

                    // Row 1
                    SH("CNSS Part Patronale"); SH("AMO Part Patronale");
                    SH("CIMR Part Patronale"); SH("Mutuelle Part Patronale");
                    SH("Brut Imposable"); SH("Net Imposable");

                    SV(F(payroll.CnssPartPatronale) ?? "0.00");
                    SV(F(payroll.AmoPartPatronale) ?? "0.00");
                    SV(F(payroll.CimrPartPatronale) ?? "0.00");
                    SV(F(payroll.MutuellePartPatronale) ?? "0.00");
                    SV(F(payroll.BrutImposable) ?? "0.00");
                    SV(F(payroll.NetImposable) ?? "0.00");

                    // Row 2
                    SH("IR"); SH("Total Cotis. Sal.");
                    SH("Total Cotis. Pat."); SH("Total Retenues");
                    SH("Avance sur Salaire"); SH("Intérêt Logement");

                    SV(F(payroll.ImpotRevenu) ?? "0.00");
                    SV(F(payroll.TotalCotisationsSalariales) ?? "0.00");
                    SV(F(payroll.TotalCotisationsPatronales) ?? "0.00");
                    SV(F(payroll.TotalRetenues) ?? "0.00");
                    SV(F(payroll.AvanceSurSalaire) ?? "0.00");
                    SV(F(payroll.InteretSurLogement) ?? "0.00");
                });
            }

            // =====================================================================
            // LEAVE BALANCE
            // LeaveBalance confirmed: CarryInDays, CarryOutDays, UsedDays
            // AcquiredDays treated as nullable-safe
            // =====================================================================
            void ComposeLeaveBalance(IContainer container)
            {
                container.Background(Colors.Blue.Lighten5).Padding(8).Column(col =>
                {
                    col.Item().Text("SOLDE DE CONGÉS").Bold().FontSize(9);
                    col.Item().PaddingVertical(4);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Solde reporté (N-1) : {leaveBalance.CarryInDays:N2} j");
                            // AcquiredDays: guard with null-coalescing
                            decimal acquired = (leaveBalance.AccruedDays);
                            c.Item().Text($"Acquis ce mois : {acquired:N2} j");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Jours pris : {leaveBalance.UsedDays:N2} j");
                            c.Item().Text($"Solde reporté (N+1) : {leaveBalance.CarryOutDays:N2} j");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            decimal available = leaveBalance.CarryInDays
                                + (leaveBalance.AccruedDays)
                                - leaveBalance.UsedDays;
                            c.Item().Text($"Solde disponible : {available:N2} j").Bold();
                        });
                    });
                });
            }

            // =====================================================================
            // TABLE HELPERS
            // =====================================================================
            void AddRow(TableDescriptor table, string label, string? baseVal, string? rate,
                        string? gain, string? retenue, bool isBold = false)
            {
                var style = isBold ? TextStyle.Default.Bold() : TextStyle.Default;
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(label).Style(style);
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(baseVal ?? "").AlignRight();
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(rate ?? "").AlignRight();
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(gain ?? "").AlignRight();
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(retenue ?? "").AlignRight();
            }

            void AddSeparatorRow(TableDescriptor table)
            {
                table.Cell().ColumnSpan(5).PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            }
        }

        /// <summary>Formats a nullable decimal as "N2". Returns null if the value is null.</summary>
        private static string? F(decimal? value) => value?.ToString("N2");

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
}*/