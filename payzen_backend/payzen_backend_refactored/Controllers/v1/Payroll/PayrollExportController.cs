using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using payzen_backend.Services.Payroll;
using System.Globalization;
using System.Text;

namespace payzen_backend.Controllers.v1.Payroll
{
    /// <summary>
    /// Exports de paie marocaine : Journal de Paie, État CNSS (Damancom), État IR
    /// Tous les endpoints nécessitent une authentification.
    /// </summary>
    [Route("api/v{version:apiVersion}/payroll/exports")]
    [ApiController]
    [ApiVersion("1.0")]
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
                HasHeaderRecord = true
            };
            using var csv = new CsvWriter(writer, csvConfig);

            // ─── En-têtes ──────────────────────────────────────────────────
            csv.WriteField("Matricule");
            csv.WriteField("Nom & Prénom");
            csv.WriteField("CIN");
            csv.WriteField("N° CNSS");
            csv.WriteField("Salaire Base");
            csv.WriteField("Total Brut");
            csv.WriteField("Cotisations Sal.");
            csv.WriteField("IR Retenu");
            csv.WriteField("Net à Payer");
            csv.WriteField("Détail Primes");
            await csv.NextRecordAsync();

            // ─── Données ───────────────────────────────────────────────────
            foreach (var r in rows)
            {
                csv.WriteField(r.Matricule);
                csv.WriteField(r.NomPrenom);
                csv.WriteField(r.CIN);
                csv.WriteField(r.CNSS);
                csv.WriteField(r.SalaireBase.ToString("F2", CultureInfo.GetCultureInfo("fr-FR")));
                csv.WriteField(r.TotalBrut.ToString("F2", CultureInfo.GetCultureInfo("fr-FR")));
                csv.WriteField(r.CotisationsSalariales.ToString("F2", CultureInfo.GetCultureInfo("fr-FR")));
                csv.WriteField(r.IR.ToString("F2", CultureInfo.GetCultureInfo("fr-FR")));
                csv.WriteField(r.NetAPayer.ToString("F2", CultureInfo.GetCultureInfo("fr-FR")));
                csv.WriteField(r.DetailsPrimes);
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