using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.Interfaces;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Import;

public class ImportTemplateService : IImportTemplateService
{
    private readonly AppDbContext _db;

    public ImportTemplateService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<(byte[] Content, string FileName)>> GenerateNewEmployeeTemplateAsync(
        int? companyId,
        int? userId,
        CancellationToken ct = default
    )
    {
        if (!userId.HasValue)
            return ServiceResult<(byte[] Content, string FileName)>.Fail("Utilisateur requis.");

        var currentUser = await _db.Users
            .AsNoTracking()
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive && u.DeletedAt == null, ct);
        if (currentUser?.Employee == null)
            return ServiceResult<(byte[] Content, string FileName)>.Fail("L'utilisateur n'est pas associé à un employé.");

        var targetCompanyId = companyId ?? currentUser.Employee.CompanyId;
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == targetCompanyId && c.DeletedAt == null, ct);
        if (company == null)
            return ServiceResult<(byte[] Content, string FileName)>.Fail("Société cible introuvable.");

        var departments = await _db.Departements.AsNoTracking()
            .Where(d => d.CompanyId == targetCompanyId && d.DeletedAt == null)
            .OrderBy(d => d.DepartementName)
            .Select(d => d.DepartementName)
            .ToListAsync(ct);

        var jobPositions = await _db.JobPositions.AsNoTracking()
            .Where(p => p.CompanyId == targetCompanyId && p.DeletedAt == null)
            .OrderBy(p => p.Name)
            .Select(p => p.Name)
            .ToListAsync(ct);

        var contractTypes = await _db.ContractTypes.AsNoTracking()
            .Where(c => c.CompanyId == targetCompanyId && c.DeletedAt == null)
            .OrderBy(c => c.ContractTypeName)
            .Select(c => c.ContractTypeName)
            .ToListAsync(ct);

        var managers = await _db.Employees.AsNoTracking()
            .Where(e => e.CompanyId == targetCompanyId && e.DeletedAt == null)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => $"{e.FirstName} {e.LastName}")
            .ToListAsync(ct);

        var genders = await _db.Genders.AsNoTracking()
            .Where(g => g.DeletedAt == null)
            .OrderBy(g => g.NameFr)
            .Select(g => g.NameFr)
            .ToListAsync(ct);

        var countries = await _db.Countries.AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.CountryName)
            .Select(c => new { c.Id, c.CountryName })
            .ToListAsync(ct);

        var cities = await _db.Cities.AsNoTracking()
            .Where(c => c.DeletedAt == null && countries.Select(x => x.Id).Contains(c.CountryId))
            .OrderBy(c => c.CityName)
            .Select(c => new { c.CountryId, c.CityName })
            .ToListAsync(ct);

        var educationLevels = await _db.EducationLevels.AsNoTracking()
            .Where(e => e.DeletedAt == null)
            .OrderBy(e => e.LevelOrder)
            .ThenBy(e => e.NameFr)
            .Select(e => e.NameFr)
            .ToListAsync(ct);

        var maritalStatuses = await _db.MaritalStatuses.AsNoTracking()
            .Where(m => m.DeletedAt == null)
            .OrderBy(m => m.NameFr)
            .Select(m => m.NameFr)
            .ToListAsync(ct);

        var situation_familiale = await _db.MaritalStatuses.AsNoTracking()
            .Where(m => m.DeletedAt == null)
            .OrderBy(m => m.NameFr)
            .Select(m => m.NameFr)
            .ToListAsync(ct);

        using var wb = new XLWorkbook();

        // Le logic de creation du template Commence ICI

        var ws = wb.Worksheets.Add("Nouveau _employés");

        var headers = new[]
        {
            "Prénom",
            "Nom",
            "CIN",
            "Genre",
            "Téléphone",
            "Email Personnel",
            "Date de naissance",
            "Situation Familiale",
            "Education",
            "Département",
            "Poste",
            "Manager",
            "Type de contrat",
            "Date d'entrée",
            "Salaire",
            "CNSS",
            "CIMR",
            "Pays",
            "Ville",
        };

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        headerRange.Style.Font.FontSize = 16;

        ws.Column(7).Style.DateFormat.Format = "dd/MM/yyyy";
        ws.Column(14).Style.DateFormat.Format = "dd/MM/yyyy";
        ws.Columns().AdjustToContents();
        ws.Rows().AdjustToContents();

        var listSheet = wb.Worksheets.Add("_lists");
        FillListColumn(listSheet, 1, "Departements", departments);
        FillListColumn(listSheet, 2, "Postes", jobPositions);
        FillListColumn(listSheet, 3, "TypesContrat", contractTypes);
        FillListColumn(listSheet, 4, "Managers", managers);
        FillListColumn(listSheet, 5, "Genres", genders);
        FillListColumn(listSheet, 6, "SituationsFamiliales", maritalStatuses);
        FillListColumn(listSheet, 7, "NiveauxEducation", educationLevels);
        FillListColumn(listSheet, 8, "Pays", countries.Select(c => c.CountryName).ToList());

        var citySheet = wb.Worksheets.Add("_cities");
        citySheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var citiesByCountry = cities
            .GroupBy(c => c.CountryId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CityName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList());

        var cityCol = 1;
        foreach (var country in countries)
        {
            if (!citiesByCountry.TryGetValue(country.Id, out var countryCities) || countryCities.Count == 0)
                continue;

            citySheet.Cell(1, cityCol).Value = country.CountryName;
            for (var i = 0; i < countryCities.Count; i++)
                citySheet.Cell(i + 2, cityCol).Value = countryCities[i];

            var cityRange = citySheet.Range(2, cityCol, countryCities.Count + 1, cityCol);
            wb.DefinedNames.Add(ToExcelRangeName(country.CountryName), cityRange);
            cityCol++;
        }

        listSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        AddValidation(ws, "D2:D500", listSheet, 5, genders.Count);
        AddValidation(ws, "H2:H500", listSheet, 6, maritalStatuses.Count);
        AddValidation(ws, "I2:I500", listSheet, 7, educationLevels.Count);
        AddValidation(ws, "J2:J500", listSheet, 1, departments.Count);
        AddValidation(ws, "K2:K500", listSheet, 2, jobPositions.Count);
        AddValidation(ws, "L2:L500", listSheet, 4, managers.Count);
        AddValidation(ws, "M2:M500", listSheet, 3, contractTypes.Count);
        AddValidation(ws, "R2:R500", listSheet, 8, countries.Count);
        AddDependentCityValidation(ws, "S2:S500", "R");

        // Pour chaque nouvelle feuille, c'est ici qu'on commence

        var changesSheet = wb.Worksheets.Add("Employees Changes");

        var changesHeaders = new[]
        {
            "Matricule",
            "Nom",
            "Prénom",
            "Date de naissance",
            "Genre",
            "Téléphone",
            "Email Personnel",
            "Date d'entrée",
            "Salaire",
            "CNSS",
            "CIMR",
            "Pays",
            "Ville",
            "Education",
            "Situation Familiale",
            "Manager",
            "Poste",
            "Type de contrat",
        };

        for (var i = 0; i < changesHeaders.Length; i++)
            changesSheet.Cell(1, i + 1).Value = changesHeaders[i];

        var headerRangeChanges = changesSheet.Range(1, 1, 1, changesHeaders.Length);
        headerRangeChanges.Style.Font.Bold = true;
        headerRangeChanges.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        headerRangeChanges.Style.Font.FontSize = 16;

        var employeesChanges = await _db.Employees.AsNoTracking()
            .Where(e => e.CompanyId == targetCompanyId && e.DeletedAt == null)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new { e.Matricule, e.LastName})
            .ToListAsync(ct);

        var changesRow = 2;
        foreach (var employee in employeesChanges)
        {
            // Les lignes Excel sont 1-based : on écrit séquentiellement pour éviter
            // d'utiliser une valeur métier (matricule) comme numéro de ligne.
            changesSheet.Cell(changesRow, 1).Value = employee.Matricule?.ToString() ?? string.Empty;
            changesSheet.Cell(changesRow, 2).Value = employee.LastName ?? string.Empty;
            changesRow++;
        }

        var absenceSheet = wb.Worksheets.Add("Absence");
        absenceSheet.Cell(1, 1).Value = "Template Absence (à implémenter)";

        var leaveSheet = wb.Worksheets.Add("Congé");
        leaveSheet.Cell(1, 1).Value = "Template Congé à implemneter";

        var overtimeSheet = wb.Worksheets.Add("Heurs Sup");
        overtimeSheet.Cell(1, 1).Value = "Template Heurs Sup à implemneter";

        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        var fileName = $"template_import{Slugify(company.CompanyName)}_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return ServiceResult<(byte[] Content, string FileName)>.Ok((ms.ToArray(), fileName));
    }

    private static void FillListColumn(IXLWorksheet ws, int col, string title, IReadOnlyList<string> values)
    {
        ws.Cell(1, col).Value = title;
        for (var i = 0; i < values.Count; i++)
            ws.Cell(i + 2, col).Value = values[i];
    }

    private static void AddValidation(IXLWorksheet targetSheet, string rangeAddress, IXLWorksheet listSheet, int listCol, int count)
    {
        if (count <= 0)
            return;

        var colLetter = XLHelper.GetColumnLetterFromNumber(listCol);
        var formula = $"='{listSheet.Name}'!${colLetter}$2:${colLetter}${count + 1}";

        var validation = targetSheet.Range(rangeAddress).CreateDataValidation();
        validation.List(formula, true);
        validation.IgnoreBlanks = true;
        validation.InCellDropdown = true;
    }

    private static void AddDependentCityValidation(IXLWorksheet targetSheet, string rangeAddress, string countryColumnLetter)
    {
        var formula = $"INDIRECT(\"cities_\"&SUBSTITUTE(${countryColumnLetter}2,\" \",\"_\"))";
        var validation = targetSheet.Range(rangeAddress).CreateDataValidation();
        validation.List(formula, true);
        validation.IgnoreBlanks = true;
        validation.InCellDropdown = true;
    }

    private static string ToExcelRangeName(string countryName)
    {
        var safe = new string(countryName
            .Where(ch => char.IsLetterOrDigit(ch) || ch == ' ')
            .Select(ch => ch == ' ' ? '_' : ch)
            .ToArray());

        if (string.IsNullOrWhiteSpace(safe))
            safe = "country";

        if (char.IsDigit(safe[0]))
            safe = $"_{safe}";

        return $"cities_{safe}";
    }

    private static string Slugify(string value)
    {
        var safe = new string(value.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ').ToArray()).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(safe) ? "company" : safe.Replace(' ', '_');
    }
}
