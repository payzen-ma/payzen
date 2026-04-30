using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.DTOs.Import;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Company;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Import;

public class NewEmployeeImportService : INewEmployeeImportService
{
    private const int MaxImportRows = 1000;
    private static readonly Regex NameRegex = new(@"^[a-zA-ZÀ-ÿ\s\-']+$", RegexOptions.Compiled);
    private readonly AppDbContext _db;
    private readonly IEmployeeService _employeeService;
    private readonly IExcelImportValidationService _excelValidationService;

    public NewEmployeeImportService(
        AppDbContext db,
        IEmployeeService employeeService,
        IExcelImportValidationService excelValidationService
    )
    {
        _db = db;
        _employeeService = employeeService;
        _excelValidationService = excelValidationService;
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
        var gendersByName = await _db
            .Genders.AsNoTracking()
            .Where(g => g.DeletedAt == null && g.IsActive)
            .Select(g => new { g.Id, g.NameFr })
            .ToListAsync(ct);
        var gendersByNameMap = gendersByName
            .GroupBy(g => Normalize(g.NameFr))
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .ToDictionary(g => g.Key, g => g.First().Id);
        var maritalStatusesByName = await _db
            .MaritalStatuses.AsNoTracking()
            .Where(m => m.DeletedAt == null)
            .Select(m => m.NameFr)
            .ToListAsync(ct);
        var maritalStatusesByNameMap = maritalStatusesByName
            .Select(Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet();
        var educationLevelsByName = await _db
            .EducationLevels.AsNoTracking()
            .Where(e => e.DeletedAt == null)
            .Select(e => e.NameFr)
            .ToListAsync(ct);
        var educationLevelsByNameMap = educationLevelsByName
            .Select(Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet();
        var countriesByName = await _db
            .Countries.AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .Select(c => new { c.Id, c.CountryName })
            .ToDictionaryAsync(c => Normalize(c.CountryName), c => c.Id, ct);
        var validCities = await _db
            .Cities.AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .Select(c => new { c.CountryId, c.CityName })
            .ToListAsync(ct);
        var cityByCountryAndName = validCities
            .ToDictionary(c => (c.CountryId, Normalize(c.CityName)), c => true);
        var existingEmployeesIdentity = await _db
            .Employees.AsNoTracking()
            .Where(e => e.CompanyId == targetCompanyId && e.DeletedAt == null)
            .Select(e => new { e.CinNumber, e.Email })
            .ToListAsync(ct);
        var existingCinKeys = existingEmployeesIdentity
            .Select(e => Normalize(e.CinNumber))
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet();
        var existingEmailKeys = existingEmployeesIdentity
            .Select(e => NormalizeEmail(e.Email))
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet();
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
        var totalDataRows = Math.Max(0, lastRowNum - headerRowNum);
        if (!_excelValidationService.ValidateMaxRows(totalDataRows, MaxImportRows, out var maxRowsError))
            return ServiceResult<NewEmployeeImportResultDto>.Fail(maxRowsError!);

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
            var genderRaw = GetCell(row, map, "Genre");
            var departmentRaw = GetCell(row, map, "Département", "Departement", "Department");
            var jobPositionRaw = GetCell(row, map, "Poste", "Emploi", "JobPosition", "Fonction");
            var contractTypeRaw = GetCell(row, map, "Type de contrat", "ContractType", "Contrat");
            var startDateRaw = GetCell(row, map, "Date d'entrée", "Date entree", "StartDate", "Date de Début");
            var salaryRaw = GetCell(row, map, "Salaire", "Salaire de Base", "BaseSalary");
            var cnssRaw = GetCell(row, map, "CNSS", "Cnss");
            var cimrRaw = GetCell(row, map, "CIMR", "Cimr");
            var ribRaw = GetCell(row, map, "RIB", "Rib", "RibNumber");
            var categoryRaw = GetCell(row, map, "Catégorie", "Category");
            var manager = GetCell(row, map, "Manager");
            var maritalStatusRaw = GetCell(row, map, "Situation Familiale");
            var educationRaw = GetCell(row, map, "Education");
            var countryRaw = GetCell(row, map, "Pays");
            var cityRaw = GetCell(row, map, "Ville");
            DateTime MaritalStatusChangeDate = DateTime.Now;
            DateTime? ManagerChangeDate = DateTime.Now;
            DateTime? CategoryChangeDate = DateTime.Now;



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

            if (_excelValidationService.ContainsPotentialFormula(firstName) || _excelValidationService.ContainsPotentialFormula(lastName) || _excelValidationService.ContainsPotentialFormula(cin)
                || _excelValidationService.ContainsPotentialFormula(phone) || _excelValidationService.ContainsPotentialFormula(email) || _excelValidationService.ContainsPotentialFormula(dateOfBirthRaw)
                || _excelValidationService.ContainsPotentialFormula(genderRaw) || _excelValidationService.ContainsPotentialFormula(departmentRaw) || _excelValidationService.ContainsPotentialFormula(jobPositionRaw)
                || _excelValidationService.ContainsPotentialFormula(contractTypeRaw) || _excelValidationService.ContainsPotentialFormula(startDateRaw) || _excelValidationService.ContainsPotentialFormula(salaryRaw)
                || _excelValidationService.ContainsPotentialFormula(cnssRaw) || _excelValidationService.ContainsPotentialFormula(cimrRaw) || _excelValidationService.ContainsPotentialFormula(ribRaw)
                || _excelValidationService.ContainsPotentialFormula(categoryRaw) || _excelValidationService.ContainsPotentialFormula(manager) || _excelValidationService.ContainsPotentialFormula(maritalStatusRaw)
                || _excelValidationService.ContainsPotentialFormula(educationRaw) || _excelValidationService.ContainsPotentialFormula(countryRaw) || _excelValidationService.ContainsPotentialFormula(cityRaw))
            {
                result.ErrorCount++;
                result.Errors.Add(new NewEmployeeImportErrorDto
                {
                    Row = r,
                    Message = "Valeur invalide: une formule Excel a été détectée dans la ligne."
                });
                continue;
            }

            if (!_excelValidationService.ValidateRequiredText(firstName, "Prénom", 100, NameRegex, out var firstNameError))
            {
                result.ErrorCount++;
                result.Errors.Add(new NewEmployeeImportErrorDto
                {
                    Row = r,
                    Message = firstNameError!
                });
                continue;
            }

            if (!_excelValidationService.ValidateRequiredText(lastName, "Nom", 100, NameRegex, out var lastNameError))
            {
                result.ErrorCount++;
                result.Errors.Add(new NewEmployeeImportErrorDto
                {
                    Row = r,
                    Message = lastNameError!
                });
                continue;
            }

            if (!_excelValidationService.ValidateOptionalEmail(email, "Email personnel", 254, out var emailError))
            {
                result.ErrorCount++;
                result.Errors.Add(new NewEmployeeImportErrorDto { Row = r, Message = emailError! });
                continue;
            }

            var cinKey = Normalize(cin);
            if (!string.IsNullOrWhiteSpace(cinKey) && existingCinKeys.Contains(cinKey))
            {
                // Doublon détecté: on ignore silencieusement la ligne.
                continue;
            }

            var emailKey = NormalizeEmail(email);
            if (!string.IsNullOrWhiteSpace(emailKey) && existingEmailKeys.Contains(emailKey))
            {
                // Doublon détecté: on ignore silencieusement la ligne.
                continue;
            }

            int? departementId = null;
            if (!string.IsNullOrWhiteSpace(departmentRaw))
            {
                var depKey = Normalize(departmentRaw);
                if (!departementsByName.TryGetValue(depKey, out var depId))
                {
                    var newDepartment = new Departement
                    {
                        DepartementName = departmentRaw.Trim(),
                        CompanyId = targetCompanyId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = userId.Value
                    };
                    _db.Departements.Add(newDepartment);
                    await _db.SaveChangesAsync(ct);
                    depId = newDepartment.Id;
                    departementsByName[depKey] = depId;
                    result.CreatedDepartmentsCount++;
                }
                departementId = depId;
            }

            int? jobPositionId = null;
            if (!string.IsNullOrWhiteSpace(jobPositionRaw))
            {
                var jobKey = Normalize(jobPositionRaw);
                if (!jobPositionsByName.TryGetValue(jobKey, out var jobId))
                {
                    var newJobPosition = new JobPosition
                    {
                        Name = jobPositionRaw.Trim(),
                        CompanyId = targetCompanyId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = userId.Value
                    };
                    _db.JobPositions.Add(newJobPosition);
                    await _db.SaveChangesAsync(ct);
                    jobId = newJobPosition.Id;
                    jobPositionsByName[jobKey] = jobId;
                    result.CreatedJobPositionsCount++;
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
            var ribNumber = ParseDecimal(ribRaw);

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

            int? genderId = null;
            if (!string.IsNullOrWhiteSpace(genderRaw))
            {
                var genderKey = Normalize(genderRaw);
                if (!gendersByNameMap.TryGetValue(genderKey, out var genderIdValue))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new NewEmployeeImportErrorDto { Row = r, Message = $"Genre introuvable: '{genderRaw}'." });
                    continue;
                }
                genderId = genderIdValue;
            }

            if (!string.IsNullOrWhiteSpace(maritalStatusRaw) && !maritalStatusesByNameMap.Contains(Normalize(maritalStatusRaw)))
            {
                result.ErrorCount++;
                result.Errors.Add(new NewEmployeeImportErrorDto
                {
                    Row = r,
                    Message = $"Situation familiale invalide: '{maritalStatusRaw}'."
                });
                continue;
            }

            if (!string.IsNullOrWhiteSpace(educationRaw) && !educationLevelsByNameMap.Contains(Normalize(educationRaw)))
            {
                result.ErrorCount++;
                result.Errors.Add(new NewEmployeeImportErrorDto
                {
                    Row = r,
                    Message = $"Niveau d'éducation invalide: '{educationRaw}'."
                });
                continue;
            }

            int? countryId = null;
            if (!string.IsNullOrWhiteSpace(countryRaw))
            {
                if (!countriesByName.TryGetValue(Normalize(countryRaw), out var countryIdValue))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new NewEmployeeImportErrorDto
                    {
                        Row = r,
                        Message = $"Pays invalide: '{countryRaw}'."
                    });
                    continue;
                }
                countryId = countryIdValue;
            }

            if (!string.IsNullOrWhiteSpace(cityRaw))
            {
                if (!countryId.HasValue)
                {
                    result.ErrorCount++;
                    result.Errors.Add(new NewEmployeeImportErrorDto
                    {
                        Row = r,
                        Message = "Ville fournie sans pays associé."
                    });
                    continue;
                }

                if (!cityByCountryAndName.ContainsKey((countryId.Value, Normalize(cityRaw))))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new NewEmployeeImportErrorDto
                    {
                        Row = r,
                        Message = $"Ville invalide pour le pays '{countryRaw}': '{cityRaw}'."
                    });
                    continue;
                }
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
                GenderId = genderId,
                CompanyId = targetCompanyId,
                StatusId = activeStatus.Id,
                DepartementId = departementId,
                JobPositionId = hasCompleteContract ? jobPositionId : null,
                ContractTypeId = hasCompleteContract ? contractTypeId : null,
                StartDate = hasCompleteContract ? startDate : null,
                Salary = salary,
                CnssNumber = string.IsNullOrWhiteSpace(cnssRaw) ? null : cnssRaw.Trim(),
                CimrNumber = string.IsNullOrWhiteSpace(cimrRaw) ? null : cimrRaw.Trim(),
                RibNumber = ribNumber,
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
            if (!string.IsNullOrWhiteSpace(cinKey))
                existingCinKeys.Add(cinKey);
            if (!string.IsNullOrWhiteSpace(emailKey))
                existingEmailKeys.Add(emailKey);
            result.AddedEmployees.Add(new NewEmployeeImportSuccessDto
            {
                Row = r,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim()
            });
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

    private static string NormalizeEmail(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        return input.Trim().ToLowerInvariant();
    }

    private static string? Clean(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var s = raw.Trim().Trim('"', '\'', '=').Replace('\u00A0', ' ').Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

}
