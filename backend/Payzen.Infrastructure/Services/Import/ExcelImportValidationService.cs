using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Payzen.Application.Interfaces;

namespace Payzen.Infrastructure.Services.Import;

public class ExcelImportValidationService : IExcelImportValidationService
{
    public bool ValidateMaxRows(int dataRows, int maxRows, out string? error)
    {
        error = null;
        if (dataRows <= maxRows)
            return true;

        error = $"Fichier trop volumineux: {dataRows} lignes détectées (maximum autorisé: {maxRows}).";
        return false;
    }

    public bool ContainsPotentialFormula(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var s = value.TrimStart();
        return s.StartsWith("=") || s.StartsWith("+") || s.StartsWith("-") || s.StartsWith("@");
    }

    public bool ValidateRequiredText(string? value, string fieldName, int maxLength, Regex allowedRegex, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"{fieldName} obligatoire.";
            return false;
        }

        if (value.Length > maxLength)
        {
            error = $"{fieldName} trop long (max {maxLength} caractères).";
            return false;
        }

        if (!allowedRegex.IsMatch(value))
        {
            error = $"{fieldName} invalide.";
            return false;
        }

        return true;
    }

    public bool ValidateOptionalEmail(string? value, string fieldName, int maxLength, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (value.Length > maxLength)
        {
            error = $"{fieldName} trop long (max {maxLength} caractères).";
            return false;
        }

        if (!new EmailAddressAttribute().IsValid(value))
        {
            error = $"{fieldName} invalide.";
            return false;
        }

        return true;
    }
}
