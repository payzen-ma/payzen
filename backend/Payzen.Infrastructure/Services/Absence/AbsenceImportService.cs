using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Employee;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Absence;

/// <summary>
/// Service d'importation des absences depuis fichiers XLSX/CSV.
/// </summary>
public class AbsenceImportService : IAbsenceImportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AbsenceImportService> _logger;

    public AbsenceImportService(AppDbContext db, ILogger<AbsenceImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult<AbsenceImportResultDto>> ImportAbsencesFromFileAsync(
        Stream fileStream,
        string fileName,
        int? userId,
        CancellationToken ct = default
    )
    {
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".csv")
            return ServiceResult<AbsenceImportResultDto>.Fail("Le fichier doit être au format XLSX ou CSV.");

        // Déterminer la société (basé sur l'utilisateur)
        int targetCompanyId;
        if (userId.HasValue)
        {
            var currentUser = await _db
                .Users.AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive && u.DeletedAt == null, ct);
            if (currentUser?.Employee == null)
                return ServiceResult<AbsenceImportResultDto>.Fail("L'utilisateur n'est pas associé à un employé.");
            targetCompanyId = currentUser.Employee.CompanyId;
        }
        else
        {
            return ServiceResult<AbsenceImportResultDto>.Fail("Utilisateur requis pour déterminer la société.");
        }

        var result = new AbsenceImportResultDto
        {
            TotalLines = 0,
            SuccessCount = 0,
            ErrorCount = 0,
            ImportedAbsences = new List<AbsenceImportRowDto>(),
            Errors = new List<AbsenceImportErrorDto>(),
            Sheets = new List<AbsenceImportSheetDto>(),
            EmployeeChecks = new List<AbsenceEmployeeCheckDto>()
        };

        // Charger les employés de la société
        var employeesByMatricule = await _db
            .Employees.AsNoTracking()
            .Where(e => e.CompanyId == targetCompanyId && e.Matricule != null && e.DeletedAt == null)
            .ToDictionaryAsync(e => e.Matricule!.Value, ct);

        // Parser le fichier
        var parsedAbsences = new List<ParsedAbsence>();

        try
        {
            if (ext == ".xlsx")
                await ParseXlsxAbsences(fileStream, parsedAbsences, result.Sheets);
            else
                await ParseCsvAbsences(fileStream, parsedAbsences, result.Sheets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du parsing du fichier d'absences");
            return ServiceResult<AbsenceImportResultDto>.Fail("Erreur lors de la lecture du fichier.");
        }

        result.TotalLines = parsedAbsences.Count;
        if (parsedAbsences.Count == 0)
            return ServiceResult<AbsenceImportResultDto>.Ok(result);

        // Traiter chaque absence
        var createdAbsences = new List<(EmployeeAbsence Absence, AbsenceImportRowDto Preview, string SheetName)>();
        var now = DateTimeOffset.UtcNow;

        foreach (var parsed in parsedAbsences)
        {
            // Valider le matricule
            if (string.IsNullOrWhiteSpace(parsed.MatriculeRaw))
            {
                result.EmployeeChecks.Add(new AbsenceEmployeeCheckDto
                {
                    Row = parsed.RowIndex,
                    Matricule = null,
                    Exists = false,
                    IsLastNameMatch = false,
                    IsFirstNameMatch = false,
                    Message = "Matricule manquant."
                });
                result.ErrorCount++;
                result.Errors.Add(new AbsenceImportErrorDto
                {
                    Row = parsed.RowIndex,
                    Matricule = null,
                    Message = "Matricule manquant."
                });
                continue;
            }

            if (!int.TryParse(parsed.MatriculeRaw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var matricule))
            {
                result.EmployeeChecks.Add(new AbsenceEmployeeCheckDto
                {
                    Row = parsed.RowIndex,
                    Matricule = parsed.MatriculeRaw,
                    Exists = false,
                    IsLastNameMatch = false,
                    IsFirstNameMatch = false,
                    Message = "Matricule invalide."
                });
                result.ErrorCount++;
                result.Errors.Add(new AbsenceImportErrorDto
                {
                    Row = parsed.RowIndex,
                    Matricule = parsed.MatriculeRaw,
                    Message = "Matricule invalide."
                });
                continue;
            }

            // Trouver l'employé
            if (!employeesByMatricule.TryGetValue(matricule, out var employee))
            {
                result.EmployeeChecks.Add(new AbsenceEmployeeCheckDto
                {
                    Row = parsed.RowIndex,
                    Matricule = parsed.MatriculeRaw,
                    Exists = false,
                    IsLastNameMatch = false,
                    IsFirstNameMatch = false,
                    Message = $"Aucun employé avec le matricule {matricule}."
                });
                result.ErrorCount++;
                result.Errors.Add(new AbsenceImportErrorDto
                {
                    Row = parsed.RowIndex,
                    Matricule = parsed.MatriculeRaw,
                    Message = $"Aucun employé avec le matricule {matricule}."
                });
                continue;
            }

            // Valider la cohérence matricule + nom + prénom (si fournis dans le fichier)
            var isLastNameMatch = IsNameMatch(parsed.Nom, employee.LastName);
            var isFirstNameMatch = IsNameMatch(parsed.Prenom, employee.FirstName);
            result.EmployeeChecks.Add(new AbsenceEmployeeCheckDto
            {
                Row = parsed.RowIndex,
                Matricule = parsed.MatriculeRaw,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                Exists = true,
                IsLastNameMatch = isLastNameMatch,
                IsFirstNameMatch = isFirstNameMatch,
                Message = isLastNameMatch && isFirstNameMatch
                    ? "Employé trouvé et identité cohérente."
                    : "Employé trouvé, mais nom/prénom incohérent."
            });

            if (!isLastNameMatch)
            {
                result.ErrorCount++;
                result.Errors.Add(new AbsenceImportErrorDto
                {
                    Row = parsed.RowIndex,
                    Matricule = parsed.MatriculeRaw,
                    Message = $"Nom incohérent pour le matricule {matricule}."
                });
                continue;
            }

            if (!isFirstNameMatch)
            {
                result.ErrorCount++;
                result.Errors.Add(new AbsenceImportErrorDto
                {
                    Row = parsed.RowIndex,
                    Matricule = parsed.MatriculeRaw,
                    Message = $"Prénom incohérent pour le matricule {matricule}."
                });
                continue;
            }

            // Valider la date
            if (!parsed.AbsenceDate.HasValue)
            {
                result.ErrorCount++;
                result.Errors.Add(new AbsenceImportErrorDto
                {
                    Row = parsed.RowIndex,
                    Matricule = parsed.MatriculeRaw,
                    Message = "Date d'absence invalide."
                });
                continue;
            }

            // Créer l'absence et préparer l'aperçu
            var absence = new EmployeeAbsence
            {
                EmployeeId = employee.Id,
                AbsenceDate = parsed.AbsenceDate.Value,
                DurationType = parsed.DurationType,
                Reason = parsed.Reason,
                Status = AbsenceStatus.Submitted, // À approuver
                CreatedAt = now,
                CreatedBy = userId ?? 0
            };

            var preview = new AbsenceImportRowDto
            {
                Row = parsed.RowIndex,
                Matricule = parsed.MatriculeRaw.Trim(),
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                AbsenceDate = parsed.AbsenceDate.Value.ToString("dd/MM/yyyy"),
                DurationType = parsed.DurationType.ToString(),
                Reason = parsed.Reason,
                Status = "Créée"
            };

            createdAbsences.Add((absence, preview, parsed.SheetName ?? "Inconnue"));
        }

        var absencesToCreate = createdAbsences.Select(x => x.Absence).ToList();

        // Vérifier les doublons (même employé + même date)
        var existingAbsences = await _db.EmployeeAbsences
            .Where(a => absencesToCreate.Select(x => x.EmployeeId).Contains(a.EmployeeId))
            .Where(a => absencesToCreate.Select(x => x.AbsenceDate).Contains(a.AbsenceDate))
            .ToListAsync(ct);

        var duplicates = createdAbsences
            .Where(x => existingAbsences.Any(existing =>
                existing.EmployeeId == x.Absence.EmployeeId &&
                existing.AbsenceDate == x.Absence.AbsenceDate))
            .ToList();

        if (duplicates.Any())
        {
            foreach (var duplicate in duplicates)
            {
                result.ErrorCount++;
                result.Errors.Add(new AbsenceImportErrorDto
                {
                    Row = duplicate.Preview.Row,
                    Matricule = duplicate.Preview.Matricule,
                    Message = "Absence déjà existante pour cette date."
                });
            }

            createdAbsences.RemoveAll(x => duplicates.Contains(x));
            absencesToCreate = createdAbsences.Select(x => x.Absence).ToList();
        }

        result.SuccessCount = absencesToCreate.Count;
        result.ImportedAbsences = createdAbsences.Select(x => x.Preview).ToList();
        foreach (var sheet in result.Sheets)
        {
            sheet.ImportedLines = createdAbsences.Count(x =>
                string.Equals(x.SheetName, sheet.SheetName, StringComparison.OrdinalIgnoreCase));
        }

        // Sauvegarder en base
        if (absencesToCreate.Any())
        {
            await _db.EmployeeAbsences.AddRangeAsync(absencesToCreate, ct);
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Absence import OK — company {CompanyId} success={Success} errors={Errors}",
            targetCompanyId,
            result.SuccessCount,
            result.ErrorCount
        );

        return ServiceResult<AbsenceImportResultDto>.Ok(result);
    }

    private static async Task ParseXlsxAbsences(
        Stream stream,
        List<ParsedAbsence> absences,
        List<AbsenceImportSheetDto> sheets
    )
    {
        using var wb = new XLWorkbook(stream);
        foreach (var ws in wb.Worksheets)
        {
            var firstRow = ws.FirstRowUsed();
            var sheetSummary = new AbsenceImportSheetDto
            {
                SheetName = ws.Name,
                ReadLines = 0,
                ImportedLines = 0
            };
            sheets.Add(sheetSummary);

            if (firstRow == null)
                continue;

            var headerRowNum = firstRow.RowNumber();
            var lastRow = ws.LastRowUsed();
            if (lastRow == null)
                continue;

            var lastRowNum = lastRow.RowNumber();
            var headerMap = BuildHeaderMap(ws.Row(headerRowNum));

            for (var r = headerRowNum + 1; r <= lastRowNum; r++)
            {
                var row = ws.Row(r);
                var matricule = GetCell(row, headerMap, "Matricule");
                var nom = GetCell(row, headerMap, "Nom");
                var prenom = GetCell(row, headerMap, "Prénom", "Prenom");
                var dateAbsence = GetCell(row, headerMap, "Date d'Absence", "Date Absence");
                var typeDuree = GetCell(row, headerMap, "Type de Durée", "Type Duree");
                var motif = GetCell(row, headerMap, "Motif de l'absence", "Motif");

                if (string.IsNullOrWhiteSpace(matricule) && string.IsNullOrWhiteSpace(dateAbsence))
                    continue;

                var parsed = new ParsedAbsence
                {
                    RowIndex = r,
                    MatriculeRaw = matricule,
                    Nom = nom,
                    Prenom = prenom,
                    AbsenceDate = ParseDate(dateAbsence),
                    DurationType = ParseDurationType(typeDuree),
                    Reason = motif,
                    SheetName = ws.Name
                };

                absences.Add(parsed);
                sheetSummary.ReadLines++;
            }
        }
    }

    private static async Task ParseCsvAbsences(
        Stream stream,
        List<ParsedAbsence> absences,
        List<AbsenceImportSheetDto> sheets
    )
    {
        var sheetSummary = new AbsenceImportSheetDto
        {
            SheetName = "CSV",
            ReadLines = 0,
            ImportedLines = 0
        };
        sheets.Add(sheetSummary);

        using var reader = new StreamReader(stream);
        var headerLine = await reader.ReadLineAsync();
        if (headerLine == null) return;

        var delim = headerLine.Contains('\t') ? '\t' : ';';
        var headers = headerLine.Split(delim);
        var headerMap = BuildHeaderMap(headers);
        var rowIdx = 1;

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            rowIdx++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = line.Split(delim);
            var matricule = GetCell(cells, headerMap, "Matricule");
            var nom = GetCell(cells, headerMap, "Nom");
            var prenom = GetCell(cells, headerMap, "Prénom", "Prenom");
            var dateAbsence = GetCell(cells, headerMap, "Date d'Absence", "Date Absence");
            var typeDuree = GetCell(cells, headerMap, "Type de Durée", "Type Duree");
            var motif = GetCell(cells, headerMap, "Motif de l'absence", "Motif");

            if (string.IsNullOrWhiteSpace(matricule) && string.IsNullOrWhiteSpace(dateAbsence))
                continue;

            var parsed = new ParsedAbsence
            {
                RowIndex = rowIdx,
                MatriculeRaw = matricule,
                Nom = nom,
                Prenom = prenom,
                AbsenceDate = ParseDate(dateAbsence),
                DurationType = ParseDurationType(typeDuree),
                Reason = motif,
                SheetName = "CSV"
            };

            absences.Add(parsed);
            sheetSummary.ReadLines++;
        }
    }

    private static DateOnly? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return null;

        // Essayer différents formats de date
        var formats = new[] { "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy" };
        foreach (var format in formats)
        {
            if (DateOnly.TryParseExact(dateStr.Trim(), format, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            {
                return date;
            }
        }
        return null;
    }

    private static AbsenceDurationType ParseDurationType(string? typeStr)
    {
        if (string.IsNullOrWhiteSpace(typeStr)) return AbsenceDurationType.FullDay;

        var normalized = typeStr.Trim().ToLowerInvariant();
        return normalized switch
        {
            "demi-journée" or "half-day" or "demi journee" => AbsenceDurationType.HalfDay,
            "heure" or "hour" => AbsenceDurationType.Hourly,
            _ => AbsenceDurationType.FullDay
        };
    }

    // Méthodes utilitaires (similaires à TimesheetImportService)
    private static Dictionary<string, int> BuildHeaderMap(IXLRow row)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in row.CellsUsed())
        {
            var key = Normalize(cell.GetString());
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                map[key] = cell.Address.ColumnNumber - 1;
        }
        return map;
    }

    private static Dictionary<string, int> BuildHeaderMap(string[] headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            var key = Normalize(headers[i]);
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                map[key] = i;
        }
        return map;
    }

    private static string? GetCell(IXLRow row, Dictionary<string, int> map, params string[] candidates)
    {
        foreach (var c in candidates)
        {
            if (map.TryGetValue(Normalize(c), out var idx))
            {
                var v = Clean(row.Cell(idx + 1).GetString());
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            }
        }
        return null;
    }

    private static string? GetCell(string[] cells, Dictionary<string, int> map, params string[] candidates)
    {
        foreach (var c in candidates)
        {
            if (map.TryGetValue(Normalize(c), out var idx) && idx >= 0 && idx < cells.Length)
            {
                var v = Clean(cells[idx]);
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            }
        }
        return null;
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var s = input.Trim().Trim('"', '\'', '=');
        // Simplification : convertir en minuscules et garder seulement lettres/chiffres/espaces
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ')
                sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString().Trim();
    }

    private static bool IsNameMatch(string? imported, string? expected)
    {
        if (string.IsNullOrWhiteSpace(imported))
            return true;

        return NormalizePersonName(imported) == NormalizePersonName(expected);
    }

    private static string NormalizePersonName(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
        }

        return sb.ToString();
    }

    private static string? Clean(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim().Trim('"', '\'', '=').Replace('\u00A0', ' ').Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private class ParsedAbsence
    {
        public int RowIndex { get; set; }
        public string? MatriculeRaw { get; set; }
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public DateOnly? AbsenceDate { get; set; }
        public AbsenceDurationType DurationType { get; set; }
        public string? Reason { get; set; }
        public string? SheetName { get; set; }
    }
}