using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Import;
using Payzen.Application.Interfaces;

namespace Payzen.Infrastructure.Services.Import;

public class ModuleImportService : IModuleImportService
{
    private readonly INewEmployeeImportService _newEmployeeImportService;

    public ModuleImportService(
        INewEmployeeImportService newEmployeeImportService
    )
    {
        _newEmployeeImportService = newEmployeeImportService;
    }


    public async Task<ServiceResult<ModuleImportResultDto>> ImportWorkbookAsync(
        Stream fileStream,
        string fileName,
        int month,
        int year,
        string mode,
        int? half,
        int? companyId,
        int? userId,
        bool sendWelcomeEmail,
        CancellationToken ct = default
    )
    {
        if (fileStream == null || !fileStream.CanRead)
            return ServiceResult<ModuleImportResultDto>.Fail("Fichier invalide.");
        if (Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() != ".xlsx")
            return ServiceResult<ModuleImportResultDto>.Fail("Le fichier doit être au format Excel .xlsx.");

        if (fileStream.CanSeek)
            fileStream.Position = 0;

        using var workbook = new XLWorkbook(fileStream);
        var result = new ModuleImportResultDto
        {
            TotalSheets = workbook.Worksheets.Count
        };

        foreach (var sheet in workbook.Worksheets)
        {
            ct.ThrowIfCancellationRequested();
            var normalized = Normalize(sheet.Name);

            if (IsNewEmployeeSheet(normalized))
            {
                var sheetStream = BuildWorkbookStream(sheet);
                var r = await _newEmployeeImportService.ImportFromFileAsync(
                    sheetStream,
                    $"{sheet.Name}.xlsx",
                    companyId,
                    userId,
                    sendWelcomeEmail,
                    ct
                );
                AddSheetResult(
                    result,
                    sheet.Name,
                    "nouveaux_employes",
                    r.Success,
                    r.Error ?? "Import nouveaux employés terminé.",
                    r.Data
                );
                continue;
            }

            if (IsEmployeeChangesSheet(normalized))
            {
                result.SkippedSheets++;
                result.Sheets.Add(new ModuleImportSheetResultDto
                {
                    SheetName = sheet.Name,
                    SheetType = "employees_changes",
                    Success = true,
                    Message = "Feuille détectée mais non encore traitée (service dédié à implémenter)."
                });
                continue;
            }

            var headerHint = DetectSheetTypeFromHeader(sheet);
            if (headerHint == "nouveaux_employes")
            {
                var sheetStream = BuildWorkbookStream(sheet);
                var r = await _newEmployeeImportService.ImportFromFileAsync(
                    sheetStream,
                    $"{sheet.Name}.xlsx",
                    companyId,
                    userId,
                    sendWelcomeEmail,
                    ct
                );
                AddSheetResult(
                    result,
                    sheet.Name,
                    "nouveaux_employes",
                    r.Success,
                    r.Error ?? "Import nouveaux employés terminé.",
                    r.Data
                );
                continue;
            }

            if (headerHint == "employees_changes")
            {
                result.SkippedSheets++;
                result.Sheets.Add(new ModuleImportSheetResultDto
                {
                    SheetName = sheet.Name,
                    SheetType = "employees_changes",
                    Success = true,
                    Message = "Feuille détectée mais non encore traitée (service dédié à implémenter)."
                });
                continue;
            }

            result.SkippedSheets++;
            result.Sheets.Add(new ModuleImportSheetResultDto
            {
                SheetName = sheet.Name,
                SheetType = "inconnu",
                Success = false,
                Message = "Feuille ignorée: aucun service d'import associé."
            });
        }

        return ServiceResult<ModuleImportResultDto>.Ok(result);
    }

    private static MemoryStream BuildWorkbookStream(IXLWorksheet sourceSheet)
    {
        var ms = new MemoryStream();
        using (var singleSheetWorkbook = new XLWorkbook())
        {
            sourceSheet.CopyTo(singleSheetWorkbook, sourceSheet.Name);
            singleSheetWorkbook.SaveAs(ms);
        }
        ms.Position = 0;
        return ms;
    }

    private static void AddSheetResult(
        ModuleImportResultDto result,
        string sheetName,
        string sheetType,
        bool success,
        string message,
        NewEmployeeImportResultDto? details = null
    )
    {
        result.ProcessedSheets++;
        if (!success)
            result.FailedSheets++;

        result.Sheets.Add(new ModuleImportSheetResultDto
        {
            SheetName = sheetName,
            SheetType = sheetType,
            Success = success,
            Message = message,
            TotalRows = details?.TotalRows ?? 0,
            SuccessCount = details?.SuccessCount ?? 0,
            ErrorCount = details?.ErrorCount ?? 0,
            CreatedDepartmentsCount = details?.CreatedDepartmentsCount ?? 0,
            CreatedJobPositionsCount = details?.CreatedJobPositionsCount ?? 0,
            AddedEmployees = details?.AddedEmployees ?? new List<NewEmployeeImportSuccessDto>(),
            Errors = details?.Errors ?? new List<NewEmployeeImportErrorDto>()
        });
    }

    private static bool IsAbsenceSheet(string normalizedName) =>
        normalizedName.Contains("absence");

    private static bool IsTimesheetSheet(string normalizedName) =>
        normalizedName.Contains("pointage");

    private static bool IsChangementSheet(string normalizedName) =>
        normalizedName.Contains("changement");

    private static bool IsEmployeeChangesSheet(string normalizedName) =>
        normalizedName.Contains("employeechanges");

    private static bool IsNewEmployeeSheet(string normalizedName) =>
        normalizedName.Contains("nouveauemploy")
        || normalizedName.Contains("newemployee");

    private static string? DetectSheetTypeFromHeader(IXLWorksheet sheet)
    {
        var firstRow = sheet.FirstRowUsed();
        if (firstRow == null)
            return null;

        var headers = firstRow
            .CellsUsed()
            .Select(c => Normalize(c.GetString()))
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .ToList();
        if (headers.Count == 0)
            return null;

        var joined = string.Join(" ", headers);
        if (
            joined.Contains("datenaissance")
            || joined.Contains("cin")
            || joined.Contains("telephone")
            || joined.Contains("cnss")
            || joined.Contains("cimr")
            || joined.Contains("rib")
        )
        {
            if (joined.Contains("matricule"))
                return "employees_changes";
            return "nouveaux_employes";
        }

        return null;
    }

    private static string Normalize(string? input)
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
}
