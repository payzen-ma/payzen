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

namespace Payzen.Infrastructure.Services.Timesheet;

/// <summary>
/// Import de pointage XLSX/CSV (format Sage) et lecture des pointages.
/// Toute la logique métier et l'accès aux données sont ici (Clean Architecture).
/// </summary>
public class TimesheetImportService : ITimesheetImportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TimesheetImportService> _logger;

    public TimesheetImportService(AppDbContext db, ILogger<TimesheetImportService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<ServiceResult<TimesheetImportResultDto>> ImportFromFileAsync(
        Stream fileStream,
        string fileName,
        int month,
        int year,
        string mode,
        int? half,
        int? companyId,
        int? userId,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".csv")
            return ServiceResult<TimesheetImportResultDto>.Fail("Le fichier doit être au format XLSX ou CSV.");

        int targetCompanyId;
        if (userId.HasValue)
        {
            var currentUser = await _db.Users.AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive && u.DeletedAt == null, ct);
            if (currentUser?.Employee == null)
                return ServiceResult<TimesheetImportResultDto>.Fail("L'utilisateur n'est pas associé à un employé.");
            targetCompanyId = companyId ?? currentUser.Employee.CompanyId;
        }
        else
        {
            if (!companyId.HasValue)
                return ServiceResult<TimesheetImportResultDto>.Fail("companyId est requis.");
            targetCompanyId = companyId.Value;
        }

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == targetCompanyId && c.DeletedAt == null, ct);
        if (company == null)
            return ServiceResult<TimesheetImportResultDto>.Fail("Société non trouvée.");

        var result = new TimesheetImportResultDto { Month = month, Year = year, PeriodMode = mode };
        var employeesByMatricule = await _db.Employees.AsNoTracking()
            .Where(e => e.CompanyId == targetCompanyId && e.Matricule != null && e.DeletedAt == null)
            .ToDictionaryAsync(e => e.Matricule!.Value, ct);

        var parsedRows = new List<(int RowIndex, string? MatriculeRaw, decimal WorkedHours)>();

        try
        {
            if (ext == ".xlsx") await ParseXlsx(fileStream, parsedRows, result);
            else await ParseCsv(fileStream, parsedRows, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur parsing timesheet");
            return ServiceResult<TimesheetImportResultDto>.Fail("Erreur lors de la lecture du fichier.");
        }

        result.TotalLines = parsedRows.Count;
        if (parsedRows.Count == 0) return ServiceResult<TimesheetImportResultDto>.Ok(result);

        var startOfMonth = new DateOnly(year, month, 1);
        var endOfMonth   = startOfMonth.AddMonths(1).AddDays(-1);
        DateOnly periodStart, periodEnd;

        if (mode == "bi_monthly")
        {
            periodStart = half == 1 ? startOfMonth : startOfMonth.AddDays(15);
            periodEnd   = half == 1 ? startOfMonth.AddDays(14) : endOfMonth;
            result.Half = half;
        }
        else { periodStart = startOfMonth; periodEnd = endOfMonth; }

        var hoursByEmployeeId = new Dictionary<int, decimal>();
        foreach (var row in parsedRows)
        {
            if (string.IsNullOrWhiteSpace(row.MatriculeRaw))
            {
                result.ErrorCount++;
                result.Errors.Add(new TimesheetImportErrorDto { Row = row.RowIndex, Matricule = null, Message = "Matricule manquant." });
                continue;
            }
            if (!int.TryParse(row.MatriculeRaw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var matricule))
            {
                result.ErrorCount++;
                result.Errors.Add(new TimesheetImportErrorDto { Row = row.RowIndex, Matricule = row.MatriculeRaw, Message = "Matricule invalide." });
                continue;
            }
            if (!employeesByMatricule.TryGetValue(matricule, out var emp))
            {
                result.ErrorCount++;
                result.Errors.Add(new TimesheetImportErrorDto { Row = row.RowIndex, Matricule = row.MatriculeRaw, Message = $"Aucun employé avec le matricule {matricule}." });
                continue;
            }
            hoursByEmployeeId.TryGetValue(emp.Id, out var cur);
            hoursByEmployeeId[emp.Id] = cur + row.WorkedHours;
            result.SuccessCount++;
        }

        if (hoursByEmployeeId.Count == 0) return ServiceResult<TimesheetImportResultDto>.Ok(result);

        var existing = await _db.EmployeeAttendances
            .Where(a => hoursByEmployeeId.Keys.Contains(a.EmployeeId)
                     && a.WorkDate >= periodStart && a.WorkDate <= periodEnd
                     && a.Source == AttendanceSource.Manual)
            .ToListAsync(ct);
        if (existing.Count > 0) _db.EmployeeAttendances.RemoveRange(existing);

        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in hoursByEmployeeId)
            _db.EmployeeAttendances.Add(new EmployeeAttendance
            {
                EmployeeId          = kvp.Key,
                WorkDate            = periodStart,
                WorkedHours         = kvp.Value,
                BreakMinutesApplied = 0,
                Status              = AttendanceStatus.Present,
                Source              = AttendanceSource.Manual,
                CreatedAt           = now,
                CreatedBy           = userId ?? 0
            });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Timesheet import OK — company {CompanyId} {Month}/{Year} success={S} errors={E}",
            targetCompanyId, month, year, result.SuccessCount, result.ErrorCount);
        return ServiceResult<TimesheetImportResultDto>.Ok(result);
    }

    public async Task<ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>> GetTimesheetsAsync(
        int month,
        int year,
        int? companyId,
        int? userId,
        CancellationToken ct = default)
    {
        int targetCompanyId;
        if (userId.HasValue)
        {
            var u = await _db.Users.AsNoTracking().Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == userId.Value && x.IsActive && x.DeletedAt == null, ct);
            if (u?.Employee == null)
                return ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>.Fail("Utilisateur non associé à un employé.");
            targetCompanyId = companyId ?? u.Employee.CompanyId;
        }
        else
        {
            if (!companyId.HasValue)
                return ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>.Fail("companyId est requis.");
            targetCompanyId = companyId.Value;
        }

        var start = new DateOnly(year, month, 1);
        var end   = start.AddMonths(1).AddDays(-1);

        var list = await _db.EmployeeAttendances.AsNoTracking()
            .Include(a => a.Employee).Include(a => a.Breaks)
            .Where(a => a.Employee!.CompanyId == targetCompanyId
                     && a.WorkDate >= start && a.WorkDate <= end)
            .OrderBy(a => a.Employee!.FirstName).ThenBy(a => a.WorkDate)
            .Select(a => new EmployeeAttendanceReadDto
            {
                Id                  = a.Id,
                EmployeeId          = a.EmployeeId,
                EmployeeName        = a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}" : null,
                WorkDate            = a.WorkDate,
                CheckIn             = a.CheckIn,
                CheckOut            = a.CheckOut,
                WorkedHours         = a.WorkedHours,
                BreakMinutesApplied = a.BreakMinutesApplied,
                Status              = a.Status,
                Source              = a.Source,
                Breaks = a.Breaks!.Select(b => new EmployeeAttendanceBreakReadDto
                {
                    Id         = b.Id,
                    BreakStart = b.BreakStart,
                    BreakEnd   = b.BreakEnd,
                    BreakType  = b.BreakType ?? string.Empty,
                    CreatedAt  = b.CreatedAt,
                    ModifiedAt = b.UpdatedAt
                }).ToList()
            })
            .ToListAsync(ct);

        return ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>.Ok(list);
    }

    // ── Parsers (détail d'implémentation) ─────────────────────────────────────

    private static async Task ParseXlsx(Stream stream,
        List<(int, string?, decimal)> rows, TimesheetImportResultDto result)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet(1);
        var firstRow = ws.FirstRowUsed();
        if (firstRow == null) return;
        var headerRowNum = firstRow.RowNumber();
        var lastRowNum   = ws.LastRowUsed().RowNumber();
        var headerMap    = BuildHeaderMap(ws.Row(headerRowNum));

        for (var r = headerRowNum + 1; r <= lastRowNum; r++)
        {
            var row = ws.Row(r);
            var mat  = GetCell(row, headerMap, "MAT", "Matricule", "Mat");
            var nrh  = GetCell(row, headerMap, "NR H", "NRH", "NB H", "HEURES");
            if (string.IsNullOrWhiteSpace(mat) && string.IsNullOrWhiteSpace(nrh)) continue;
            if (!TryParseDecimal(nrh, out var h) || h < 0)
            { result.ErrorCount++; result.Errors.Add(new() { Row = r, Matricule = mat, Message = $"Heures invalides : '{nrh}'." }); continue; }
            rows.Add((r, mat, h));
        }
        await Task.CompletedTask;
    }

    private static async Task ParseCsv(Stream stream,
        List<(int, string?, decimal)> rows, TimesheetImportResultDto result)
    {
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
            var mat = GetCell(cells, headerMap, "MAT", "Matricule", "Mat");
            var nrh = GetCell(cells, headerMap, "NR H", "NRH", "NB H", "HEURES");
            if (string.IsNullOrWhiteSpace(mat) && string.IsNullOrWhiteSpace(nrh)) continue;
            if (!TryParseDecimal(nrh, out var h) || h < 0)
            { result.ErrorCount++; result.Errors.Add(new() { Row = rowIdx, Matricule = mat, Message = $"Heures invalides : '{nrh}'." }); continue; }
            rows.Add((rowIdx, mat, h));
        }
    }

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
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key)) map[key] = i;
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
                if (!string.IsNullOrWhiteSpace(v)) return v;
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
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
        }
        return null;
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim().Trim('"', '\'', '=');
        var norm = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in norm)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }

    private static string? Clean(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim().Trim('"', '\'', '=').Replace('\u00A0', ' ').Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static bool TryParseDecimal(string? input, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var s = input.Trim().Replace('\u00A0', ' ').Trim();
        var lastDot = s.LastIndexOf('.');
        var lastComma = s.LastIndexOf(',');
        char dec = (lastDot >= 0 && lastComma >= 0) ? (lastComma > lastDot ? ',' : '.') : lastComma >= 0 ? ',' : '.';
        var norm = dec == ','
            ? s.Replace(".", "").Replace(" ", "").Replace(',', '.')
            : s.Replace(",", "").Replace(" ", "");
        if (norm.StartsWith("(") && norm.EndsWith(")")) norm = "-" + norm[1..^1];
        if (norm.StartsWith("+")) norm = norm[1..];
        return decimal.TryParse(norm, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
            || decimal.TryParse(s, NumberStyles.Any, new CultureInfo("fr-FR"), out value);
    }
}
