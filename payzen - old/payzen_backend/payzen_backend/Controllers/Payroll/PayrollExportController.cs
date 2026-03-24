using CsvHelper;
using CsvHelper.Configuration;
using IronPdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using payzen_backend.DTOs.Payroll;
using payzen_backend.Services.Payroll;
using System.Globalization;
using System.Text;

namespace payzen_backend.Controllers.Payroll
{
    /// <summary>
    /// Exports de paie marocaine : Journal de Paie, État CNSS (Damancom), État IR
    /// Tous les endpoints nécessitent une authentification.
    /// </summary>
    [Route("api/payroll/exports")]
    [ApiController]
    [Authorize]
    public class PayrollExportController : ControllerBase
    {
        private readonly IPayrollExportService _exportService;
        private readonly ILogger<PayrollExportController> _logger;

        public PayrollExportController(
            IPayrollExportService exportService,
            ILogger<PayrollExportController> logger)
        {
            _exportService = exportService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Journal de Paie → CSV
        // GET /api/payroll/exports/journal/{companyId}/{year}/{month}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Génère le Journal de Paie mensuel au format CSV (UTF-8 BOM, point-virgule).
        /// </summary>
        [HttpGet("journal/{companyId:int}/{year:int}/{month:int}")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadJournalPaie(int companyId, int year, int month)
        {
            if (!ValidateParams(companyId, year, month, out var error))
                return BadRequest(new { message = error });

            var rows = await _exportService.GetJournalPaie(companyId, year, month);
            if (rows.Count == 0)
                return NotFound(new { message = $"Aucun bulletin validé pour {month:D2}/{year}." });

            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, encoding);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = false  // On écrit manuellement 2 lignes d'en-têtes
            };
            using var csv = new CsvWriter(writer, csvConfig);
            var frCulture = CultureInfo.GetCultureInfo("fr-FR");

            // ─── Ligne 1 des en-têtes ──────────────────────────────────────
            csv.WriteField("Matricule");
            csv.WriteField("Nom");
            csv.WriteField("Prénom");
            csv.WriteField("Date Naissance");
            csv.WriteField("Nbr Jrs Travaillés");
            csv.WriteField("Jrs Feriés");
            csv.WriteField("SB + CONGE");
            csv.WriteField("salaire de base du mois");
            csv.WriteField("Nbr deductions");
            csv.WriteField("CNSS part salariale");
            csv.WriteField("AMO part salariale");
            csv.WriteField("Frais profes à déduire M");
            csv.WriteField("IR A PAYER");
            csv.WriteField("SALAIRE NET");
            csv.WriteField("INTERET Logement");
            csv.WriteField("Heures normales");
            csv.WriteField("Heures sup 50%");
            await csv.NextRecordAsync();

            // ─── Ligne 2 des en-têtes ──────────────────────────────────────
            csv.WriteField("SF");
            csv.WriteField("N° CIN");
            csv.WriteField("N° CNSS");
            csv.WriteField("Date Embauche");
            csv.WriteField("Fonction");
            csv.WriteField("Nbr Jrs Congé");
            csv.WriteField("MT Ancienneté");
            csv.WriteField("Les Primes imposables");
            csv.WriteField("Brut imposable");
            csv.WriteField("Mutuelle Part Salariale");
            csv.WriteField("CIMR");
            csv.WriteField("Net imposable");
            csv.WriteField("Mt exonéré");
            csv.WriteField("BRUT Global");
            csv.WriteField("AVANCE");
            csv.WriteField("Heures sup 25%");
            csv.WriteField("Heures sup 100%");
            await csv.NextRecordAsync();

            // ─── Données (2 lignes par employé) ────────────────────────────
            foreach (var r in rows)
            {
                // Ligne 1 de l'employé
                csv.WriteField(r.Matricule);
                csv.WriteField(r.Nom);
                csv.WriteField(r.Prenom);
                csv.WriteField(r.DateNaissance);
                csv.WriteField(r.NbrJrsTravailles);
                csv.WriteField(r.JrsFeries.ToString("F2", frCulture));
                csv.WriteField(r.SBPlusConge.ToString("F2", frCulture));
                csv.WriteField(r.SalaireBaseDuMois.ToString("F2", frCulture));
                csv.WriteField(r.NbrDeductions);
                csv.WriteField(r.CNSSPartSalariale.ToString("F2", frCulture));
                csv.WriteField(r.AMOPartSalariale.ToString("F2", frCulture));
                csv.WriteField(r.FraisProfesADeduireM.ToString("F2", frCulture));
                csv.WriteField(r.IRAPayer.ToString("F2", frCulture));
                csv.WriteField(r.SalaireNet.ToString("F2", frCulture));
                csv.WriteField(r.InteretLogement.ToString("F2", frCulture));
                csv.WriteField(r.HeuresNormales.ToString("F2", frCulture));
                csv.WriteField(r.HeuresSup50.ToString("F2", frCulture));
                await csv.NextRecordAsync();

                // Ligne 2 de l'employé
                csv.WriteField(r.SF);
                csv.WriteField(r.CIN);
                csv.WriteField(r.CNSS);
                csv.WriteField(r.DateEmbauche);
                csv.WriteField(r.Fonction);
                csv.WriteField(r.NbrJrsConge.ToString("F2", frCulture));
                csv.WriteField(r.MTAnciennete.ToString("F2", frCulture));
                csv.WriteField(r.LesPrimesImposables.ToString("F2", frCulture));
                csv.WriteField(r.BrutImposable.ToString("F2", frCulture));
                csv.WriteField(r.MutuellePartSalariale.ToString("F2", frCulture));
                csv.WriteField(r.CIMR.ToString("F2", frCulture));
                csv.WriteField(r.NetImposable.ToString("F2", frCulture));
                csv.WriteField(r.MtExonere.ToString("F2", frCulture));
                csv.WriteField(r.BrutGlobal.ToString("F2", frCulture));
                csv.WriteField(r.Avance.ToString("F2", frCulture));
                csv.WriteField(r.HeuresSup25.ToString("F2", frCulture));
                csv.WriteField(r.HeuresSup100.ToString("F2", frCulture));
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync();
            var bytes = ms.ToArray();
            var fileName = $"JournalPaie_{year}_{month:D2}.csv";
            _logger.LogInformation("Export Journal de Paie : {FileName} ({Count} lignes)", fileName, rows.Count);
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // État CNSS → CSV Damancom
        // GET /api/payroll/exports/cnss/{companyId}/{year}/{month}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Génère l'État CNSS au format CSV compatible Damancom (UTF-8 BOM, point-virgule).
        /// </summary>
        [HttpGet("cnss/{companyId:int}/{year:int}/{month:int}")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadEtatCnss(int companyId, int year, int month)
        {
            if (!ValidateParams(companyId, year, month, out var error))
                return BadRequest(new { message = error });

            var rows = await _exportService.GetEtatCnss(companyId, year, month);
            if (rows.Count == 0)
                return NotFound(new { message = $"Aucun salarié CNSS trouvé pour {month:D2}/{year}." });

            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, encoding);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true
            };
            using var csv = new CsvWriter(writer, csvConfig);

            // ─── En-têtes Damancom ─────────────────────────────────────────
            csv.WriteField("Nom et Prénom");
            csv.WriteField("Numéro CNSS");
            csv.WriteField("Salaire Brut Déclaré");
            csv.WriteField("Nombre de Jours");
            await csv.NextRecordAsync();

            foreach (var r in rows)
            {
                csv.WriteField(r.NomPrenom);
                csv.WriteField(r.NumeroCnss);
                csv.WriteField(r.SalaireBrutDeclare.ToString("F2", CultureInfo.GetCultureInfo("fr-FR")));
                csv.WriteField(r.NombreJoursDeclare);
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync();
            var bytes = ms.ToArray();
            var fileName = $"EtatCnss_{year}_{month:D2}.csv";
            _logger.LogInformation("Export État CNSS : {FileName} ({Count} lignes)", fileName, rows.Count);
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // État CNSS → PDF
        // GET /api/payroll/exports/cnss-pdf/{companyId}/{year}/{month}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Génère l'État CNSS au format PDF (bordereau de déclaration des cotisations CNSS Maroc).
        /// </summary>
        [HttpGet("cnss-pdf/{companyId:int}/{year:int}/{month:int}")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadEtatCnssPdf(int companyId, int year, int month)
        {
            if (!ValidateParams(companyId, year, month, out var error))
                return BadRequest(new { message = error });

            EtatCnssPdfData data;
            try
            {
                data = await _exportService.GetEtatCnssPdfData(companyId, year, month);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }

            if (data.Rows.Count == 0)
                return NotFound(new { message = $"Aucun salarié CNSS trouvé pour {month:D2}/{year}." });

            var html = BuildEtatCnssPdfHtml(data);
            var renderer = new ChromePdfRenderer();
            renderer.RenderingOptions.PaperSize       = IronPdf.Rendering.PdfPaperSize.A4;
            renderer.RenderingOptions.PaperOrientation = IronPdf.Rendering.PdfPaperOrientation.Landscape;
            renderer.RenderingOptions.MarginTop    = 10;
            renderer.RenderingOptions.MarginBottom = 10;
            renderer.RenderingOptions.MarginLeft   = 10;
            renderer.RenderingOptions.MarginRight  = 10;
            renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print;

            var document = renderer.RenderHtmlAsPdf(html);
            var pdfBytes = document.BinaryData;
            var pdfName  = $"EtatCNSS_{data.CompanyName.Replace(" ", "_")}_{year}_{month:D2}.pdf";

            _logger.LogInformation("Export État CNSS PDF : {FileName} ({Count} lignes)", pdfName, data.Rows.Count);
            return File(pdfBytes, "application/pdf", pdfName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Builder HTML — État CNSS
        // ─────────────────────────────────────────────────────────────────────

        private static readonly string[] _frMonths =
            ["Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
             "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"];

        private static string N(decimal v) =>
            v.ToString("N2", CultureInfo.GetCultureInfo("fr-FR"));

        private static string BuildEtatCnssPdfHtml(EtatCnssPdfData d)
        {
            var mois = _frMonths[d.Month - 1];

            // ── totaux page ───────────────────────────────────────────────────
            int     totalJours       = d.Rows.Sum(r => r.NombreJours);
            decimal totalBrut        = d.Rows.Sum(r => r.SalaireBrut);
            decimal totalBaseCnss    = d.Rows.Sum(r => r.BaseCnss);
            decimal totalRgSal       = d.Rows.Sum(r => r.RgSalarial);
            decimal totalAmoSal      = d.Rows.Sum(r => r.AmoSalarial);
            decimal totalRgPat       = d.Rows.Sum(r => r.RgPatronal);
            decimal totalAfPat       = d.Rows.Sum(r => r.AfPatronal);
            decimal totalFpPat       = d.Rows.Sum(r => r.FpPatronal);
            decimal totalAmoPat      = d.Rows.Sum(r => r.AmoPatronal);
            decimal totalCotisAmo    = d.Rows.Sum(r => r.CotisationAmo);
            decimal totalParticipAmo = d.Rows.Sum(r => r.ParticipationAmo);

            decimal totalPS          = totalRgSal + totalRgPat;          // PS total (sal + pat)
            decimal totalAmo         = totalCotisAmo + totalParticipAmo; // AMO total
            decimal totalAPayer      = totalPS + totalAfPat + totalFpPat;
            decimal totalGeneral     = totalAPayer + totalAmo;

            // ── lignes ────────────────────────────────────────────────────────
            var sb = new StringBuilder();
            foreach (var r in d.Rows)
            {
                // Séparer Nom et Prénom
                var parts = r.NomPrenom.Split(' ', 2);
                var nom = parts.Length > 0 ? parts[0] : "";
                var prenom = parts.Length > 1 ? parts[1] : "";

                sb.Append($@"
<tr>
  <td class='c'>{r.NumeroCnss}</td>
  <td class='l name'>{nom}</td>
  <td class='l name'>{prenom}</td>
  <td class='c'>{r.NombreJours}</td>
  <td class='r'>{N(r.SalaireBrut)}</td>
  <td class='r'>{N(r.BaseCnss)}</td>
</tr>");
            }

            return $@"<!DOCTYPE html>
<html lang=""fr"">
<head>
<meta charset=""UTF-8""/>
<style>
  * {{ margin:0; padding:0; box-sizing:border-box; }}
  body {{ font-family:'Arial',sans-serif; font-size:8pt; color:#1a1a1a; }}

  /* ── En-tête ── */
  .header {{ text-align:center; margin-bottom:12px; }}
  .doc-title {{ font-size:11pt; font-weight:bold; color:#1a1a1a; }}

  /* ── Infos entreprise ── */
  .company-block {{ display:none; }}

  /* ── Tableau ── */
  table {{ width:100%; border-collapse:collapse; font-size:9pt; }}
  thead th {{ background:#fff; color:#000; padding:6px 8px; border:1px solid #000;
              text-align:center; white-space:nowrap; font-size:8pt; font-weight:bold; }}
  tbody tr {{ background:#fff; }}
  td {{ padding:4px 8px; border:1px solid #000; vertical-align:middle; }}
  .c    {{ text-align:center; }}
  .l    {{ text-align:left; }}
  .r    {{ text-align:right; font-variant-numeric:tabular-nums; }}
  .name {{ font-weight:normal; }}

  /* ── Lignes de totaux ── */
  tr.tot-page td {{
    background:#fff; color:#000; font-weight:bold;
    border:1px solid #000; padding:6px 8px; text-transform:uppercase;
  }}
  tr.tot-cumul td {{
    background:#fff; color:#000; font-weight:bold;
    border:1px solid #000; padding:6px 8px; text-transform:uppercase;
  }}

  /* ── Récapitulatif ── */
  .recap {{ margin-top:16px; border:1px solid #000; }}
  .recap-section {{ padding:6px 10px; border-bottom:1px solid #000; background:#fff; }}
  .recap-section:last-child {{ border-bottom:none; }}
  .recap-section .sec-title {{ font-weight:bold; font-size:8.5pt; color:#000; 
                                margin-bottom:3px; text-transform:uppercase; }}
  .recap-section .line {{ display:flex; justify-content:space-between; align-items:center;
                          padding:2px 0; font-size:8pt; }}
  .recap-section .line.sub {{ padding-left:16px; font-size:7.5pt; color:#333; }}
  .recap-section .line .label {{ flex:1; }}
  .recap-section .line .taux {{ color:#555; font-size:7pt; margin-right:8px; }}
  .recap-section .line .val {{ font-weight:bold; color:#000; min-width:90px; text-align:right; }}
  .recap-section.total-final {{ background:#fff; border-top:2px solid #000; }}
  .recap-section.total-final .line {{ font-size:9pt; font-weight:bold; color:#000; }}
  .recap-section.total-final .val {{ font-size:10pt; }}

  /* ── Signature ── */
  .sig-row {{ display:none; }}
</style>
</head>
<body>

<!-- EN-TÊTE -->
<div class=""header"">
  <div class=""doc-title"">ETAT CNSS DU MOIS : {d.Month:D2}/{d.Year.ToString().Substring(2)}</div>
</div>

<!-- TABLEAU -->
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
{sb}
</tbody>
<tfoot>
  <tr class=""tot-page"">
    <td colspan=""4"" class=""l"">TOTAL PAGE ACTUELLE</td>
    <td class=""r"">{N(totalBrut)}</td>
    <td class=""r"">{N(totalBaseCnss)}</td>
  </tr>
  <tr class=""tot-cumul"">
    <td colspan=""4"" class=""l"">TOTAL CUMUL PAGE ACTUELLE ET PRECEDENTES</td>
    <td class=""r"">{N(totalBrut)}</td>
    <td class=""r"">{N(totalBaseCnss)}</td>
  </tr>
</tfoot>
</table>

<!-- RÉCAPITULATIF COTISATIONS -->
<div class=""recap"">
  
  <!-- Allocations Familiales -->
  <div class=""recap-section"">
    <div class=""sec-title"">Allocations Familiales</div>
    <div class=""line"">
      <span class=""label"">Part patronale</span>
      <span class=""taux"">(6,40 % sur brut)</span>
      <span class=""val"">{N(totalAfPat)}</span>
    </div>
  </div>

  <!-- Prestations Sociales -->
  <div class=""recap-section"">
    <div class=""sec-title"">Prestations Sociales</div>
    <div class=""line"">
      <span class=""label"">Part salariale</span>
      <span class=""taux"">(4,48 % sur base plafonnée)</span>
      <span class=""val"">{N(totalRgSal)}</span>
    </div>
    <div class=""line"">
      <span class=""label"">Part patronale</span>
      <span class=""taux"">(8,98 % sur base plafonnée)</span>
      <span class=""val"">{N(totalRgPat)}</span>
    </div>
    <div class=""line sub"">
      <span class=""label""><strong>Total Prestations Sociales</strong></span>
      <span class=""taux"">(13,46 %)</span>
      <span class=""val""><strong>{N(totalPS)}</strong></span>
    </div>
  </div>

  <!-- Formation Professionnelle -->
  <div class=""recap-section"">
    <div class=""sec-title"">Formation Professionnelle</div>
    <div class=""line"">
      <span class=""label"">Part patronale</span>
      <span class=""taux"">(1,60 % sur brut)</span>
      <span class=""val"">{N(totalFpPat)}</span>
    </div>
  </div>

  <!-- Total à Payer -->
  <div class=""recap-section"">
    <div class=""line"" style=""font-weight:bold;font-size:8.5pt;color:#000"">
      <span class=""label"">TOTAL À PAYER</span>
      <span class=""taux""></span>
      <span class=""val"" style=""font-size:9pt"">{N(totalAPayer)}</span>
    </div>
  </div>

  <!-- AMO -->
  <div class=""recap-section"">
    <div class=""sec-title"">Assurance Maladie Obligatoire</div>
    <div class=""line"">
      <span class=""label"">Cotisation A.M.O</span>
      <span class=""taux"">(4,52 % sur brut)</span>
      <span class=""val"">{N(totalCotisAmo)}</span>
    </div>
    <div class=""line sub"">
      <span class=""label"">→ dont part salariale</span>
      <span class=""taux"">(2,26 %)</span>
      <span class=""val"">{N(totalAmoSal)}</span>
    </div>
    <div class=""line sub"">
      <span class=""label"">→ dont part patronale</span>
      <span class=""taux"">(2,26 %)</span>
      <span class=""val"">{N(totalCotisAmo - totalAmoSal)}</span>
    </div>
    <div class=""line"">
      <span class=""label"">Participation A.M.O</span>
      <span class=""taux"">(1,85 % sur brut)</span>
      <span class=""val"">{N(totalParticipAmo)}</span>
    </div>
    <div class=""line sub"">
      <span class=""label""><strong>Total A.M.O</strong></span>
      <span class=""taux"">(6,37 %)</span>
      <span class=""val""><strong>{N(totalAmo)}</strong></span>
    </div>
  </div>

  <!-- Total Général -->
  <div class=""recap-section total-final"">
    <div class=""line"">
      <span class=""label"">TOTAL</span>
      <span class=""taux""></span>
      <span class=""val"">{N(totalGeneral)}</span>
    </div>
  </div>
</div>

</body>
</html>";
        }

        // ─────────────────────────────────────────────────────────────────────
        // État IR → CSV
        // GET /api/payroll/exports/ir/{companyId}/{year}/{month}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Génère l'État IR mensuel au format CSV (UTF-8 BOM, point-virgule).
        /// </summary>
        [HttpGet("ir/{companyId:int}/{year:int}/{month:int}")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadEtatIr(int companyId, int year, int month)
        {
            if (!ValidateParams(companyId, year, month, out var error))
                return BadRequest(new { message = error });

            var rows = await _exportService.GetEtatIr(companyId, year, month);
            if (rows.Count == 0)
                return NotFound(new { message = $"Aucun bulletin validé pour {month:D2}/{year}." });

            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, encoding);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true
            };
            using var csv = new CsvWriter(writer, csvConfig);

            // ─── En-têtes ──────────────────────────────────────────────────
            csv.WriteField("Nom & Prénom");
            csv.WriteField("CIN");
            csv.WriteField("N° CNSS");
            csv.WriteField("Brut Imposable");
            csv.WriteField("IR Retenu");
            await csv.NextRecordAsync();

            // ─── Données ───────────────────────────────────────────────────
            foreach (var r in rows)
            {
                csv.WriteField(r.NomPrenom);
                csv.WriteField(r.CIN);
                csv.WriteField(r.CNSS);
                csv.WriteField(r.BrutImposable.ToString("F2", CultureInfo.GetCultureInfo("fr-FR")));
                csv.WriteField(r.IRRetenu.ToString("F2", CultureInfo.GetCultureInfo("fr-FR")));
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync();
            var bytes = ms.ToArray();
            var fileName = $"EtatIR_{year}_{month:D2}.csv";
            _logger.LogInformation("Export État IR : {FileName} ({Count} lignes)", fileName, rows.Count);
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // État IR → PDF
        // GET /api/payroll/exports/ir-pdf/{companyId}/{year}/{month}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Génère l'État IR au format PDF.
        /// </summary>
        [HttpGet("ir-pdf/{companyId:int}/{year:int}/{month:int}")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadEtatIrPdf(int companyId, int year, int month)
        {
            if (!ValidateParams(companyId, year, month, out var error))
                return BadRequest(new { message = error });

            EtatIrPdfData data;
            try
            {
                data = await _exportService.GetEtatIrPdfData(companyId, year, month);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }

            if (data.Rows.Count == 0)
                return NotFound(new { message = $"Aucun bulletin validé pour {month:D2}/{year}." });

            var html = BuildEtatIrPdfHtml(data);
            var renderer = new ChromePdfRenderer();
            renderer.RenderingOptions.PaperSize       = IronPdf.Rendering.PdfPaperSize.A4;
            renderer.RenderingOptions.PaperOrientation = IronPdf.Rendering.PdfPaperOrientation.Landscape;
            renderer.RenderingOptions.MarginTop    = 10;
            renderer.RenderingOptions.MarginBottom = 10;
            renderer.RenderingOptions.MarginLeft   = 10;
            renderer.RenderingOptions.MarginRight  = 10;
            renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print;

            var document = renderer.RenderHtmlAsPdf(html);
            var pdfBytes = document.BinaryData;
            var pdfName  = $"EtatIR_{data.CompanyName.Replace(" ", "_")}_{year}_{month:D2}.pdf";

            _logger.LogInformation("Export État IR PDF : {FileName} ({Count} lignes)", pdfName, data.Rows.Count);
            return File(pdfBytes, "application/pdf", pdfName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Builder HTML — État IR
        // ─────────────────────────────────────────────────────────────────────

        private static string BuildEtatIrPdfHtml(EtatIrPdfData d)
        {
            // ── totaux ────────────────────────────────────────────────────────
            decimal totalSalImposable = d.Rows.Sum(r => r.SalImposable);
            decimal totalIGR          = d.Rows.Sum(r => r.MontantIGR);

            // ── lignes ────────────────────────────────────────────────────────
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

  /* ── En-tête entreprise ── */
  .company-header {{
    margin-bottom:16px;
    font-size:8.5pt;
    line-height:1.4;
  }}
  .company-name {{ font-weight:bold; }}

  /* ── Titre et page ── */
  .title-row {{
    display:flex;
    justify-content:center;
    align-items:center;
    position:relative;
    margin-bottom:12px;
  }}
  .doc-title {{
    font-size:11pt;
    font-weight:bold;
    text-align:center;
  }}
  .page-num {{
    position:absolute;
    right:0;
    font-size:9pt;
  }}

  /* ── Tableau ── */
  table {{ width:100%; border-collapse:collapse; font-size:9pt; margin-top:8px; }}
  thead th {{
    background:#fff;
    color:#000;
    padding:6px 8px;
    border:1px solid #000;
    text-align:center;
    font-weight:bold;
    font-size:8.5pt;
  }}
  tbody tr {{ background:#fff; }}
  td {{
    padding:5px 8px;
    border:1px solid #000;
    vertical-align:middle;
  }}
  .c {{ text-align:center; }}
  .l {{ text-align:left; }}
  .r {{ text-align:right; font-variant-numeric:tabular-nums; }}

  /* ── Ligne de total ── */
  tfoot td {{
    background:#fff;
    color:#000;
    font-weight:normal;
    border:1px solid #000;
    padding:6px 8px;
  }}
</style>
</head>
<body>

<!-- EN-TÊTE ENTREPRISE -->
<div class=""company-header"">
  <div class=""company-name"">{d.CompanyName}</div>
  <div>{d.CompanyAddress}</div>
</div>

<!-- TITRE + NUMÉRO DE PAGE -->
<div class=""title-row"">
  <div class=""doc-title"">ETAT DES PRELEVEMENTS DU MOIS &nbsp; {d.Month:D2} / {d.Year.ToString().Substring(2)}</div>
  <div class=""page-num"">1/1</div>
</div>

<!-- TABLEAU -->
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

        // ─────────────────────────────────────────────────────────────────────
        // Validation commune des paramètres
        // ─────────────────────────────────────────────────────────────────────

        private static bool ValidateParams(int companyId, int year, int month, out string error)
        {
            error = string.Empty;
            if (companyId <= 0) { error = "CompanyId invalide."; return false; }
            if (month < 1 || month > 12) { error = "Mois invalide (attendu : 1–12)."; return false; }
            if (year < 2020 || year > 2100) { error = "Année invalide."; return false; }
            return true;
        }
    }
}
