using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;

namespace payzen_backend.Controllers.Payroll
{
    [Route("api/timesheets")]
    [ApiController]
    [Authorize]
    public class TimesheetImportController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TimesheetImportController> _logger;

        public TimesheetImportController(AppDbContext db, ILogger<TimesheetImportController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Importe un fichier de pointage (XLSX ou CSV) exporté depuis Sage
        /// et alimente la table EmployeeAttendance avec les heures travaillées agrégées par période.
        /// </summary>
        /// <param name="file">Fichier de pointage (XLSX ou CSV)</param>
        /// <param name="month">Mois de paie (1-12)</param>
        /// <param name="year">Année de paie</param>
        /// <param name="mode">Mode de période : monthly ou bi_monthly</param>
        /// <param name="companyId">Société cible (optionnel, sinon société de l'utilisateur)</param>
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<TimesheetImportResultDto>> ImportTimesheet(
            IFormFile file,
            [FromQuery] int month,
            [FromQuery] int year,
            [FromQuery] string mode = "monthly",
            [FromQuery] int? half = null,
            [FromQuery] int? companyId = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Aucun fichier fourni." });

            if (month < 1 || month > 12)
                return BadRequest(new { Message = "Le mois doit être compris entre 1 et 12." });

            if (year < 2020 || year > 2100)
                return BadRequest(new { Message = "Année invalide." });

            mode = (mode ?? "monthly").Trim().ToLowerInvariant();
            if (mode != "monthly" && mode != "bi_monthly")
                return BadRequest(new { Message = "Le mode de période doit être 'monthly' ou 'bi_monthly'." });

            if (mode == "bi_monthly")
            {
                if (half == null || (half != 1 && half != 2))
                    return BadRequest(new { Message = "Le paramètre 'half' doit être 1 ou 2 pour le mode 'bi_monthly'." });
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".csv")
                return BadRequest(new { Message = "Le fichier doit être au format XLSX ou CSV." });

            // Gestion de l'utilisateur : si authentifié et avec un UserId valide,
            // on déduit la société depuis l'employé courant. Sinon, on exige un companyId explicite.
            int? userId = null;
            int targetCompanyId;

            if (User?.Identity?.IsAuthenticated == true)
            {
                try
                {
                    userId = User.GetUserId();
                }
                catch
                {
                    userId = null;
                }
            }

            if (userId.HasValue)
            {
                var currentUser = await _db.Users
                    .AsNoTracking()
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive && u.DeletedAt == null);

                if (currentUser?.Employee == null)
                    return BadRequest(new { Message = "L'utilisateur n'est pas associé à un employé." });

                targetCompanyId = companyId ?? currentUser.Employee.CompanyId;
            }
            else
            {
                if (!companyId.HasValue)
                    return BadRequest(new { Message = "companyId est requis lorsque l'utilisateur n'est pas authentifié ou que le token ne contient pas d'ID utilisateur." });

                targetCompanyId = companyId.Value;
            }

            var targetCompany = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == targetCompanyId && c.DeletedAt == null);

            if (targetCompany == null)
                return NotFound(new { Message = "Société non trouvée." });

            _logger.LogInformation(
                "Timesheet import started by user {UserId} for company {CompanyId}. Month={Month}, Year={Year}, Mode={Mode}, Half={Half}, File={FileName}",
                userId ?? 0, targetCompanyId, month, year, mode, half, file.FileName);

            var result = new TimesheetImportResultDto
            {
                Month = month,
                Year = year,
                PeriodMode = mode
            };

            // Précharger les employés de la société indexés par matricule
            var employeesByMatricule = await _db.Employees
                .AsNoTracking()
                .Where(e => e.CompanyId == targetCompanyId && e.Matricule != null && e.DeletedAt == null)
                .ToDictionaryAsync(e => e.Matricule!.Value);

            var parsedRows = new List<(int RowIndex, string? MatriculeRaw, decimal WorkedHours)>();

            try
            {
                if (ext == ".xlsx")
                {
                    await ParseXlsx(file, parsedRows, result);
                }
                else
                {
                    await ParseCsv(file, parsedRows, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du parsing du fichier de pointage.");
                return StatusCode(500, new { Message = "Erreur lors de la lecture du fichier de pointage.", Details = ex.Message });
            }

            result.TotalLines = parsedRows.Count;

            if (!parsedRows.Any())
            {
                return Ok(result);
            }

            var startOfMonth = new DateOnly(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Déterminer la période exacte (mois complet ou demi-mois)
            DateOnly periodStart;
            DateOnly periodEnd;
            if (mode == "bi_monthly")
            {
                if (half == 1)
                {
                    periodStart = startOfMonth;
                    periodEnd = startOfMonth.AddDays(14); // 1..15
                }
                else
                {
                    periodStart = startOfMonth.AddDays(15); // 16..end
                    periodEnd = endOfMonth;
                }
                result.Half = half;
            }
            else
            {
                periodStart = startOfMonth;
                periodEnd = endOfMonth;
            }

            // Pour l'instant : on agrège les heures par employé sur la période complète
            var hoursByEmployeeId = new Dictionary<int, decimal>();

            foreach (var row in parsedRows)
            {
                if (string.IsNullOrWhiteSpace(row.MatriculeRaw))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new TimesheetImportErrorDto
                    {
                        Row = row.RowIndex,
                        Matricule = null,
                        Message = "Matricule manquant."
                    });
                    continue;
                }

                if (!int.TryParse(row.MatriculeRaw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var matricule))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new TimesheetImportErrorDto
                    {
                        Row = row.RowIndex,
                        Matricule = row.MatriculeRaw,
                        Message = "Matricule invalide (format non numérique)."
                    });
                    continue;
                }

                if (!employeesByMatricule.TryGetValue(matricule, out var employee))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new TimesheetImportErrorDto
                    {
                        Row = row.RowIndex,
                        Matricule = row.MatriculeRaw,
                        Message = $"Aucun employé trouvé avec le matricule {matricule} dans la société {targetCompany.CompanyName}."
                    });
                    continue;
                }

                if (!hoursByEmployeeId.TryGetValue(employee.Id, out var current))
                {
                    current = 0m;
                }

                hoursByEmployeeId[employee.Id] = current + row.WorkedHours;
                result.SuccessCount++;
            }

            if (!hoursByEmployeeId.Any())
            {
                return Ok(result);
            }

            // On supprime les présences manuelles existantes sur la période pour ces employés afin d'éviter les doublons.
            var employeeIds = hoursByEmployeeId.Keys.ToList();

            var existingAttendances = await _db.EmployeeAttendances
                .Where(a =>
                    employeeIds.Contains(a.EmployeeId) &&
                    a.WorkDate >= periodStart &&
                    a.WorkDate <= periodEnd &&
                    a.Source == AttendanceSource.Manual)
                .ToListAsync();

            if (existingAttendances.Any())
            {
                _db.EmployeeAttendances.RemoveRange(existingAttendances);
            }

            var now = DateTimeOffset.UtcNow;

            foreach (var kvp in hoursByEmployeeId)
            {
                var attendance = new EmployeeAttendance
                {
                    EmployeeId = kvp.Key,
                    WorkDate = periodStart, // agrégat sur la période de paie (mois ou demi-mois)
                    CheckIn = null,
                    CheckOut = null,
                    BreakMinutesApplied = 0,
                    Status = AttendanceStatus.Present,
                    Source = AttendanceSource.Manual,
                    WorkedHours = kvp.Value,
                    CreatedAt = now,
                    CreatedBy = userId ?? 0 // Si null, on met 0 (système)
                };

                _db.EmployeeAttendances.Add(attendance);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Timesheet import completed for company {CompanyId}. Month={Month}, Year={Year}, Mode={Mode}, Success={Success}, Errors={Errors}",
                targetCompanyId, month, year, mode, result.SuccessCount, result.ErrorCount);

            return Ok(result);
        }

        private static async Task ParseXlsx(IFormFile file, List<(int RowIndex, string? MatriculeRaw, decimal WorkedHours)> rows, TimesheetImportResultDto result)
        {
            await using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            var firstRowUsed = worksheet.FirstRowUsed();
            if (firstRowUsed == null)
                return;

            var headerRowNumber = firstRowUsed.RowNumber();
            var lastRowNumber = worksheet.LastRowUsed().RowNumber();

            var headerMap = BuildHeaderMap(worksheet.Row(headerRowNumber));

            for (var rowNumber = headerRowNumber + 1; rowNumber <= lastRowNumber; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                var matricule = GetCellByCandidates(row, headerMap, "MAT", "Matricule", "Mat");
                var nrhStr = GetCellByCandidates(row, headerMap, "NR H", "NRH", "NB H", "HEURES");

                if (string.IsNullOrWhiteSpace(matricule) && string.IsNullOrWhiteSpace(nrhStr))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(nrhStr))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new TimesheetImportErrorDto
                    {
                        Row = rowNumber,
                        Matricule = matricule,
                        Message = "Colonne 'NR H' manquante ou vide."
                    });
                    continue;
                }

                if (!TryParseDecimalFlexible(nrhStr, out var hours) || hours < 0)
                {
                    result.ErrorCount++;
                    result.Errors.Add(new TimesheetImportErrorDto
                    {
                        Row = rowNumber,
                        Matricule = matricule,
                        Message = $"Valeur d'heures invalide : '{nrhStr}'."
                    });
                    continue;
                }

                rows.Add((rowNumber, matricule, hours));
            }
        }

        private static async Task ParseCsv(IFormFile file, List<(int RowIndex, string? MatriculeRaw, decimal WorkedHours)> rows, TimesheetImportResultDto result)
        {
            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);

            var headerLine = await reader.ReadLineAsync();
            if (headerLine == null)
                return;

            var delimiter = headerLine.Contains('\t') ? '\t' : ';';
            var headers = headerLine.Split(delimiter);

            var headerMap = BuildHeaderMap(headers);

            var rowIndex = 1;
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                rowIndex++;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var cells = line.Split(delimiter);

                var matricule = GetCellByCandidates(cells, headerMap, "MAT", "Matricule", "Mat");
                var nrhStr = GetCellByCandidates(cells, headerMap, "NR H", "NRH", "NB H", "HEURES");

                if (string.IsNullOrWhiteSpace(matricule) && string.IsNullOrWhiteSpace(nrhStr))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(nrhStr))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new TimesheetImportErrorDto
                    {
                        Row = rowIndex,
                        Matricule = matricule,
                        Message = "Colonne 'NR H' manquante ou vide."
                    });
                    continue;
                }

                if (!TryParseDecimalFlexible(nrhStr, out var hours) || hours < 0)
                {
                    result.ErrorCount++;
                    result.Errors.Add(new TimesheetImportErrorDto
                    {
                        Row = rowIndex,
                        Matricule = matricule,
                        Message = $"Valeur d'heures invalide : '{nrhStr}'."
                    });
                    continue;
                }

                rows.Add((rowIndex, matricule, hours));
            }
        }

        private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
        {
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                var raw = cell.GetString() ?? string.Empty;
                var key = NormalizeHeader(raw);
                if (!string.IsNullOrWhiteSpace(key) && !headerMap.ContainsKey(key))
                {
                    headerMap[key] = cell.Address.ColumnNumber - 1; // 0-based index
                }
            }

            return headerMap;
        }

        private static Dictionary<string, int> BuildHeaderMap(string[] headers)
        {
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                var key = NormalizeHeader(headers[i] ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(key) && !headerMap.ContainsKey(key))
                {
                    headerMap[key] = i;
                }
            }

            return headerMap;
        }

        private static string? GetCellByCandidates(IXLRow row, Dictionary<string, int> headerMap, params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                var key = NormalizeHeader(candidate);
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (headerMap.TryGetValue(key, out var index))
                {
                    var cell = row.Cell(index + 1);
                    var value = cell.GetString();
                    value = CleanCell(value);
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return null;
        }

        private static string? GetCellByCandidates(string[] cells, Dictionary<string, int> headerMap, params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                var key = NormalizeHeader(candidate);
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (headerMap.TryGetValue(key, out var index))
                {
                    if (index >= 0 && index < cells.Length)
                    {
                        var value = CleanCell(cells[index]);
                        if (!string.IsNullOrWhiteSpace(value))
                            return value;
                    }
                }
            }

            return null;
        }

        private static string NormalizeHeader(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var s = input.Trim();
            if (s.StartsWith("=\"") && s.EndsWith("\""))
                s = s.Substring(2, s.Length - 3);

            s = s.Trim('"', '\'', '=');

            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(ch))
                    sb.Append(char.ToLowerInvariant(ch));
            }

            return sb.ToString();
        }

        private static string? CleanCell(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();
            if (s.StartsWith("=\"") && s.EndsWith("\""))
                s = s.Substring(2, s.Length - 3);
            s = s.Trim('"', '\'', '=');
            s = s.Replace('\u00A0', ' ').Trim();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private static bool TryParseDecimalFlexible(string input, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(input)) return false;

            var s = input.Trim();
            // normaliser les espaces insécables
            s = s.Replace('\u00A0', ' ').Trim();

            // Détecter quel séparateur ('.' ou ',') est probablement le séparateur décimal.
            // Si les deux sont présents, on considère que le dernier caractère parmi les deux est le séparateur décimal.
            var lastDot = s.LastIndexOf('.');
            var lastComma = s.LastIndexOf(',');

            char decimalSep;
            if (lastDot >= 0 && lastComma >= 0)
            {
                decimalSep = lastComma > lastDot ? ',' : '.';
            }
            else if (lastComma >= 0)
            {
                decimalSep = ',';
            }
            else
            {
                decimalSep = '.';
            }

            // Supprimer les séparateurs de milliers ('.' ou ',' ou espaces) qui ne sont pas le séparateur décimal
            string normalized;
            if (decimalSep == ',')
            {
                normalized = s.Replace(".", string.Empty).Replace(" ", string.Empty).Replace(',', '.');
            }
            else
            {
                normalized = s.Replace(",", string.Empty).Replace(" ", string.Empty);
            }

            // Gérer les nombres entre parenthèses comme négatifs, et les signes +/
            var isNegative = false;
            if (normalized.StartsWith("(") && normalized.EndsWith(")"))
            {
                isNegative = true;
                normalized = normalized.Substring(1, normalized.Length - 2);
            }
            if (normalized.StartsWith("+")) normalized = normalized.Substring(1);

            if (decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
            {
                if (isNegative) value = -value;
                return true;
            }

            // Fallback: essayer avec fr-FR complet
            if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("fr-FR"), out value))
                return true;

            return false;
        }

        /// <summary>
        /// Récupère les pointages (timesheets) pour un mois et une année donnés
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<EmployeeAttendanceReadDto>>> GetTimesheets(
            [FromQuery] int month,
            [FromQuery] int year,
            [FromQuery] int? companyId = null)
        {
            if (month < 1 || month > 12)
                return BadRequest(new { Message = "Le mois doit être compris entre 1 et 12." });

            if (year < 2020 || year > 2100)
                return BadRequest(new { Message = "Année invalide." });

            // Déterminer la société cible
            int? userId = null;
            int targetCompanyId;

            if (User?.Identity?.IsAuthenticated == true)
            {
                try
                {
                    userId = User.GetUserId();
                }
                catch
                {
                    userId = null;
                }
            }

            if (userId.HasValue)
            {
                var currentUser = await _db.Users
                    .AsNoTracking()
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive && u.DeletedAt == null);

                if (currentUser?.Employee == null)
                    return BadRequest(new { Message = "L'utilisateur n'est pas associé à un employé." });

                targetCompanyId = companyId ?? currentUser.Employee.CompanyId;
            }
            else
            {
                if (!companyId.HasValue)
                    return BadRequest(new { Message = "Le paramètre 'companyId' est obligatoire." });

                targetCompanyId = companyId.Value;
            }

            // Calculer les dates de début et de fin du mois
            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Récupérer les pointages avec les informations des employés
            var attendances = await _db.EmployeeAttendances
                .AsNoTracking()
                .Include(a => a.Employee)
                .Include(a => a.Breaks)
                .Where(a => a.Employee!.CompanyId == targetCompanyId 
                         && a.WorkDate >= startDate 
                         && a.WorkDate <= endDate)
                .OrderBy(a => a.Employee!.FirstName)
                .ThenBy(a => a.Employee!.LastName)
                .ThenBy(a => a.WorkDate)
                .Select(a => new EmployeeAttendanceReadDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}" : null,
                    WorkDate = a.WorkDate,
                    CheckIn = a.CheckIn,
                    CheckOut = a.CheckOut,
                    WorkedHours = a.WorkedHours,
                    BreakMinutesApplied = a.BreakMinutesApplied,
                    Status = a.Status,
                    Source = a.Source,
                    Breaks = a.Breaks!.Select(b => new EmployeeAttendanceBreakReadDto
                    {
                        Id = b.Id,
                        BreakStart = b.BreakStart,
                        BreakEnd = b.BreakEnd,
                        BreakType = b.BreakType ?? string.Empty,
                        CreatedAt = b.CreatedAt,
                        ModifiedAt = b.ModifiedAt
                    }).ToList()
                })
                .ToListAsync();

            return Ok(attendances);
        }
    }
}

