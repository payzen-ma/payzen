using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
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
        // Journal de Paie → Excel
        // GET /api/payroll/exports/journal/{companyId}/{year}/{month}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Génère le Journal de Paie mensuel au format Excel (XLSX).
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

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Journal de Paie");

            // ─── En-têtes ──────────────────────────────────────────────────
            string[] headers = {
                "Matricule", "Nom &Prénom", "CIN", "N° CNSS",
                "Salaire Base", "Total Brut", "Cotisations Sal.",
                "IR Retenu", "Net à Payer", "Détail Primes"
            };
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cells[1, c + 1];
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0x1E, 0x40, 0xAF)); // bleu indigo
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // ─── Données ───────────────────────────────────────────────────
            for (int i = 8; i < rows.Count; i++)
            {
                var r   = rows[i];
                int row = i + 2;
                ws.Cells[row, 1].Value  = r.Matricule;
                ws.Cells[row, 2].Value  = r.NomPrenom;
                ws.Cells[row, 3].Value  = r.CIN;
                ws.Cells[row, 4].Value  = r.CNSS;
                ws.Cells[row, 5].Value  = r.SalaireBase;
                ws.Cells[row, 6].Value  = r.TotalBrut;
                ws.Cells[row, 7].Value  = r.CotisationsSalariales;
                ws.Cells[row, 8].Value  = r.IR;
                ws.Cells[row, 9].Value  = r.NetAPayer;
                ws.Cells[row, 10].Value = r.DetailsPrimes;

                // Alternance de couleurs lignes
                if (i % 2 == 1)
                {
                    ws.Cells[row, 1, row, headers.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, headers.Length].Style.Fill.BackgroundColor
                        .SetColor(System.Drawing.Color.FromArgb(0xEF, 0xF0, 0xFB));
                }
            }

            // ─── Format monétaire & auto-size ─────────────────────────────
            var moneyFmt = "#,##0.00";
            foreach (int col in new[] { 5, 6, 7, 8, 9 })
                ws.Cells[2, col, rows.Count + 1, col].Style.Numberformat.Format = moneyFmt;

            ws.Cells[ws.Dimension.Address].AutoFitColumns(12, 60);

            // ─── Ligne TOTAL ───────────────────────────────────────────────
            int totalRow = rows.Count + 2;
            ws.Cells[totalRow, 2].Value = "TOTAL";
            ws.Cells[totalRow, 2].Style.Font.Bold = true;
            ws.Cells[totalRow, 5].Formula = $"SUM(E2:E{rows.Count + 1})";
            ws.Cells[totalRow, 6].Formula = $"SUM(F2:F{rows.Count + 1})";
            ws.Cells[totalRow, 7].Formula = $"SUM(G2:G{rows.Count + 1})";
            ws.Cells[totalRow, 8].Formula = $"SUM(H2:H{rows.Count + 1})";
            ws.Cells[totalRow, 9].Formula = $"SUM(I2:I{rows.Count + 1})";
            ws.Cells[totalRow, 5, totalRow, 9].Style.Font.Bold = true;
            ws.Cells[totalRow, 5, totalRow, 9].Style.Numberformat.Format = moneyFmt;

            var bytes = package.GetAsByteArray();
            var fileName = $"JournalPaie_{year}_{month:D2}.xlsx";
            _logger.LogInformation("Export Journal de Paie : {FileName} ({Count} lignes)", fileName, rows.Count);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
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

            // UTF-8 BOM (requis par Damancom)
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, encoding);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter       = ";",
                HasHeaderRecord = true
            };
            using var csv = new CsvWriter(writer, csvConfig);

            // En-têtes Damancom
            csv.WriteField("Nom et Prénom");
            csv.WriteField("Numéro CNSS");
            csv.WriteField("Salaire Brut Déclaré");
            csv.WriteField("Nombre de Jours");
            await csv.NextRecordAsync();

            foreach (var r in rows)
            {
                csv.WriteField(r.NomPrenom);
                csv.WriteField(r.NumeroCnss);
                // Format décimal : virgule pour compatibilité Damancom
                csv.WriteField(r.SalaireBrutDeclare.ToString("F2", CultureInfo.GetCultureInfo("fr-FR")));
                csv.WriteField(r.NombreJoursDeclare);
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync();
            var bytes   = ms.ToArray();
            var fileName = $"EtatCnss_{year}_{month:D2}.csv";
            _logger.LogInformation("Export État CNSS : {FileName} ({Count} lignes)", fileName, rows.Count);
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // État IR → Excel
        // GET /api/payroll/exports/ir/{companyId}/{year}/{month}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Génère l'État IR mensuel au format Excel (XLSX).
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

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("État IR");

            // ─── En-têtes ──────────────────────────────────────────────────
            string[] headers = { "Nom & Prénom", "CIN", "N° CNSS", "Brut Imposable", "IR Retenu" };
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cells[1, c + 1];
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0x14, 0x53, 0x2D)); // vert foncé
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // ─── Données ───────────────────────────────────────────────────
            for (int i = 0; i < rows.Count; i++)
            {
                var r   = rows[i];
                int row = i + 2;
                ws.Cells[row, 1].Value = r.NomPrenom;
                ws.Cells[row, 2].Value = r.CIN;
                ws.Cells[row, 3].Value = r.CNSS;
                ws.Cells[row, 4].Value = r.BrutImposable;
                ws.Cells[row, 5].Value = r.IRRetenu;

                if (i % 2 == 1)
                {
                    ws.Cells[row, 1, row, headers.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, headers.Length].Style.Fill.BackgroundColor
                        .SetColor(System.Drawing.Color.FromArgb(0xF0, 0xFD, 0xF4));
                }
            }

            var moneyFmt = "#,##0.00";
            ws.Cells[2, 4, rows.Count + 1, 5].Style.Numberformat.Format = moneyFmt;
            ws.Cells[ws.Dimension.Address].AutoFitColumns(12, 60);

            // ─── Ligne TOTAL ───────────────────────────────────────────────
            int totalRow = rows.Count + 2;
            ws.Cells[totalRow, 1].Value = "TOTAL";
            ws.Cells[totalRow, 1].Style.Font.Bold = true;
            ws.Cells[totalRow, 4].Formula = $"SUM(D2:D{rows.Count + 1})";
            ws.Cells[totalRow, 5].Formula = $"SUM(E2:E{rows.Count + 1})";
            ws.Cells[totalRow, 4, totalRow, 5].Style.Font.Bold = true;
            ws.Cells[totalRow, 4, totalRow, 5].Style.Numberformat.Format = moneyFmt;

            var bytes   = package.GetAsByteArray();
            var fileName = $"EtatIR_{year}_{month:D2}.xlsx";
            _logger.LogInformation("Export État IR : {FileName} ({Count} lignes)", fileName, rows.Count);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Validation commune des paramètres
        // ─────────────────────────────────────────────────────────────────────

        private static bool ValidateParams(int companyId, int year, int month, out string error)
        {
            error = string.Empty;
            if (companyId <= 0)          { error = "CompanyId invalide.";               return false; }
            if (month < 1 || month > 12) { error = "Mois invalide (attendu : 1–12).";   return false; }
            if (year < 2020 || year > 2100) { error = "Année invalide.";                return false; }
            return true;
        }
    }
}
