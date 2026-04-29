using System.Text.RegularExpressions;

namespace Payzen.Application.Interfaces;

public interface IExcelImportValidationService
{
    bool ValidateMaxRows(int dataRows, int maxRows, out string? error);
    bool ContainsPotentialFormula(string? value);
    bool ValidateRequiredText(string? value, string fieldName, int maxLength, Regex allowedRegex, out string? error);
    bool ValidateOptionalEmail(string? value, string fieldName, int maxLength, out string? error);
}
