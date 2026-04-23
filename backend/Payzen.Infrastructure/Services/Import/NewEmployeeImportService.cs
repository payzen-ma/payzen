using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.DTOs.Import;
using Payzen.Application.Interfaces;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Import;

public class NewEmployeeImportService : INewEmployeeImportService
{
    private readonly AppDbContext _db;
    private readonly IEmployeeService _employeeService;

    public NewEmployeeImportService(AppDbContext db, IEmployeeService employeeService)
    {
        _db = db;
        _employeeService = employeeService;
    }

    public async Task<ServiceResult<NewEmployeeImportResultDto>> ImportFromFileAsync(
        Stream fileStream,
        string fileName,
        int? companyId,
        int? userId,
        bool sendWelcomeEmail,
        CancellationToken ct = default
    )
    {
        if (!userId.HasValue)
            return ServiceResult<NewEmployeeImportResultDto>.Fail(
                "Utilisateur requis pour l'import des nouveaux employés."
            );
        if (Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() != ".xlsx")
            return ServiceResult<NewEmployeeImportResultDto>.Fail("Le fichier doit être au format Excel .xlsx.");
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        var currentUser = await _db
            .Users.AsNoTracking()
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive && u.DeletedAt == null, ct);
        if (currentUser?.Employee == null)
            return ServiceResult<NewEmployeeImportResultDto>.Fail("L'utilisateur n'est pas associé à un employé.");

        var targetCompanyId = companyId ?? currentUser.Employee.CompanyId;
        var companyExists = await _db
            .Companies.AsNoTracking()
            .AnyAsync(c => c.Id == targetCompanyId && c.DeletedAt == null, ct);
        if (!companyExists)
            return ServiceResult<NewEmployeeImportResultDto>.Fail("Société cible introuvable.");

        var activeStatus = await _db
            .Statuses.AsNoTracking()
            .FirstOrDefaultAsync(
                s =>
                    s.IsActive
                    && s.DeletedAt == null
                    && (s.Code == "Active" || s.Code == "ACTIVE" || s.Code == "active"),
                ct
            );
        if (activeStatus == null)
            return ServiceResult<NewEmployeeImportResultDto>.Fail("Statut actif introuvable.");

        var departementsByName = await _db
            .Departements.AsNoTracking()
            .Where(d => d.CompanyId == targetCompanyId && d.DeletedAt == null)
            .ToDictionaryAsync(d => Normalize(d.DepartementName), d => d.Id, ct);
        var jobPositionsByName = await _db
            .JobPositions.AsNoTracking()
            .Where(j => j.CompanyId == targetCompanyId && j.DeletedAt == null)
            .ToDictionaryAsync(j => Normalize(j.Name), j => j.Id, ct);
        var contractTypesByName = await _db
            .ContractTypes.AsNoTracking()
            .Where(c => c.CompanyId == targetCompanyId && c.DeletedAt == null)
            .ToDictionaryAsync(c => Normalize(c.ContractTypeName), c => c.Id, ct);
        int? employeeRoleId = null;
        if (sendWelcomeEmail)
        {
            employeeRoleId = await _db
                .Roles.AsNoTracking()
                .Where(r => r.DeletedAt == null && (r.Name == "employee" || r.Name == "Employee"))
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(ct);

            if (!employeeRoleId.HasValue)
                return ServiceResult<NewEmployeeImportResultDto>.Fail(
                    "Rôle 'employee' introuvable pour créer les comptes utilisateurs."
                );
        }
        using var wb = new XLWorkbook(fileStream);
        var ws = wb.Worksheets.FirstOrDefault();
        if (ws == null)
            return ServiceResult<NewEmployeeImportResultDto>.Fail("Aucune feuille trouvée dans le fichier.");

        var firstRow = ws.FirstRowUsed();
        var lastRow = ws.LastRowUsed();
        if (firstRow == null || lastRow == null)
            return ServiceResult<NewEmployeeImportResultDto>.Ok(new NewEmployeeImportResultDto());

        var headerRowNum = firstRow.RowNumber();
        var lastRowNum = lastRow.RowNumber();
        var map = BuildHeaderMap(ws.Row(headerRowNum));

        var result = new NewEmployeeImportResultDto();

        for (var r = headerRowNum + 1; r <= lastRowNum; r++)
        {
            ct.ThrowIfCancellationRequested();
            var row = ws.Row(r);

            var firstName = GetCell(row, map, "Prénom");
            var lastName = GetCell(row, map, "Nom");
            var cin = GetCell(row, map, "CIN");
            var phone = GetCell(row, map, "Téléphone", "Telephone", "Phone");
            var email = GetCell(row, map, "Email Personnel");
            var dateOfBirthRaw = GetCell(row, map, "Date de naissance", "DateNaissance", "Date naissance");
            var departmentRaw = GetCell(row, map, "Département", "Departement", "Department");
            var jobPositionRaw = GetCell(row, map, "Poste", "Emploi", "JobPosition", "Fonction");
            var contractTypeRaw = GetCell(row, map, "Type de contrat", "ContractType", "Contrat");
            var startDateRaw = GetCell(row, map, "Date d'entrée", "Date entree", "StartDate", "Date de Début");
            var salaryRaw = GetCell(row, map, "Salaire", "Salaire de Base", "BaseSalary");
            var cnssRaw = GetCell(row, map, "CNSS", "Cnss");
            var cimrRaw = GetCell(row, map, "CIMR", "Cimr");

            if (
                string.IsNullOrWhiteSpace(firstName)
                && string.IsNullOrWhiteSpace(lastName)
                && string.IsNullOrWhiteSpace(cin)
            )
                continue;

            result.TotalRows++;
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                result.ErrorCount++;
                result.Errors.Add(new NewEmployeeImportErrorDto { Row = r, Message = "Prénom/Nom requis." });
                continue;
            }

            int? departementId = null;
            if (!string.IsNullOrWhiteSpace(departmentRaw))
            {
                var depKey = Normalize(departmentRaw);
                if (!departementsByName.TryGetValue(depKey, out var depId))
                {
                    result.ErrorCount++;
                    result.Errors.Add(
                        new NewEmployeeImportErrorDto
                        {
                            Row = r,
                            Message = $"Département introuvable: '{departmentRaw}'.",
                        }
                    );
                    continue;
                }
                departementId = depId;
            }

            int? jobPositionId = null;
            if (!string.IsNullOrWhiteSpace(jobPositionRaw))
            {
                var jobKey = Normalize(jobPositionRaw);
                if (!jobPositionsByName.TryGetValue(jobKey, out var jobId))
                {
                    result.ErrorCount++;
                    result.Errors.Add(
                        new NewEmployeeImportErrorDto { Row = r, Message = $"Poste introuvable: '{jobPositionRaw}'." }
                    );
                    continue;
                }
                jobPositionId = jobId;
            }

            int? contractTypeId = null;
            if (!string.IsNullOrWhiteSpace(contractTypeRaw))
            {
                var contractKey = Normalize(contractTypeRaw);
                if (!contractTypesByName.TryGetValue(contractKey, out var contractId))
                {
                    result.ErrorCount++;
                    result.Errors.Add(
                        new NewEmployeeImportErrorDto
                        {
                            Row = r,
                            Message = $"Type de contrat introuvable: '{contractTypeRaw}'.",
                        }
                    );
                    continue;
                }
                contractTypeId = contractId;
            }

            var startDate = ParseDateTime(startDateRaw);
            var salary = ParseDecimal(salaryRaw);

            var hasAnyContractField = jobPositionId.HasValue || contractTypeId.HasValue || startDate.HasValue;
            var hasCompleteContract = jobPositionId.HasValue && contractTypeId.HasValue && startDate.HasValue;
            if (hasAnyContractField && !hasCompleteContract)
            {
                result.ErrorCount++;
                result.Errors.Add(
                    new NewEmployeeImportErrorDto
                    {
                        Row = r,
                        Message = "Pour créer le contrat, il faut Poste + Type de contrat + Date d'entrée.",
                    }
                );
                var missingParts = new List<string>();
                if (!jobPositionId.HasValue)
                    missingParts.Add("Poste");
                if (!contractTypeId.HasValue)
                    missingParts.Add("TypeContrat");
                if (!startDate.HasValue)
                    missingParts.Add("DateEntree");
                continue;
            }

            if (salary.HasValue && !hasCompleteContract)
            {
                result.ErrorCount++;
                result.Errors.Add(
                    new NewEmployeeImportErrorDto
                    {
                        Row = r,
                        Message = "Le salaire nécessite un contrat complet (Poste + Type de contrat + Date d'entrée).",
                    }
                );
                continue;
            }

            var dto = new EmployeeCreateDto
            {
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                CinNumber = string.IsNullOrWhiteSpace(cin) ? $"AUTO-CIN-{targetCompanyId}-{r}" : cin.Trim(),
                DateOfBirth = ParseDate(dateOfBirthRaw) ?? new DateOnly(1990, 1, 1),
                Phone = NormalizePhone(phone),
                CountryPhoneCode = "+212",
                Email = string.IsNullOrWhiteSpace(email) ? $"import.{targetCompanyId}.{r}@import.local" : email.Trim(),
                CompanyId = targetCompanyId,
                StatusId = activeStatus.Id,
                DepartementId = departementId,
                JobPositionId = hasCompleteContract ? jobPositionId : null,
                ContractTypeId = hasCompleteContract ? contractTypeId : null,
                StartDate = hasCompleteContract ? startDate : null,
                Salary = salary,
                CnssNumber = string.IsNullOrWhiteSpace(cnssRaw) ? null : cnssRaw.Trim(),
                CimrNumber = string.IsNullOrWhiteSpace(cimrRaw) ? null : cimrRaw.Trim(),
                // Contrôle explicite de création de compte + email de bienvenue pendant l'import.
                CreateUserAccount = sendWelcomeEmail,
                InviteRoleId = sendWelcomeEmail ? employeeRoleId : null,
            };

            var created = await _employeeService.CreateAsync(dto, userId.Value, ct);
            if (!created.Success)
            {
                result.ErrorCount++;
                result.Errors.Add(
                    new NewEmployeeImportErrorDto
                    {
                        Row = r,
                        Message = created.Error ?? "Erreur lors de la création de l'employé.",
                    }
                );
                continue;
            }

            result.SuccessCount++;
        }

        return ServiceResult<NewEmployeeImportResultDto>.Ok(result);
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

    private static DateOnly? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var formats = new[] { "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy" };
        foreach (var format in formats)
        {
            if (
                DateOnly.TryParseExact(raw.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            )
                return d;
        }

        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;
        return null;
    }

    private static DateTime? ParseDateTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var s = raw.Trim();
        var formats = new[]
        {
            "dd/MM/yyyy HH:mm:ss",
            "d/M/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "d/M/yyyy HH:mm",
            "dd/MM/yyyy",
            "d/M/yyyy",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd",
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            if (DateTime.TryParseExact(s, format, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out dt))
                return dt;
        }

        if (DateTime.TryParse(s, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out var parsedFr))
            return parsedFr;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedInvariant))
            return parsedInvariant;

        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
        {
            try
            {
                return DateTime.FromOADate(oa);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static decimal? ParseDecimal(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var s = raw.Trim().Replace(" ", string.Empty);
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return value;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.GetCultureInfo("fr-FR"), out value))
            return value;
        return null;
    }

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "000000000";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length >= 9)
            return digits[^9..];
        return digits.PadLeft(9, '0');
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var s = input.Trim().Trim('"', '\'', '=');
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ')
                sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString().Trim();
    }

    private static string? Clean(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var s = raw.Trim().Trim('"', '\'', '=').Replace('\u00A0', ' ').Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
