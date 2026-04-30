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

        var categories = await _db.EmployeeCategories.AsNoTracking()
            .Where(c => c.CompanyId == targetCompanyId && c.DeletedAt == null)
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync(ct);
        var employeesChanges = await _db.Employees.AsNoTracking()
            .Where(e => e.CompanyId == targetCompanyId && e.DeletedAt == null)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new { e.Matricule, e.LastName, e.FirstName })
            .ToListAsync(ct);
        var leaveTypes = await _db.LeaveTypes.AsNoTracking()
            .Where(l => l.DeletedAt == null && l.IsActive && (l.CompanyId == null || l.CompanyId == targetCompanyId))
            .OrderBy(l => l.LeaveNameFr)
            .Select(l => l.LeaveNameFr)
            .ToListAsync(ct);

        using var wb = new XLWorkbook();

        var companySheet = wb.Worksheets.Add("Societe");
        companySheet.Cell(1, 1).Value = "Champ";
        companySheet.Cell(1, 2).Value = "Valeur";
        companySheet.Cell(2, 1).Value = "Nom";
        companySheet.Cell(2, 2).Value = company.CompanyName ?? string.Empty;
        companySheet.Cell(3, 1).Value = "Email";
        companySheet.Cell(3, 2).Value = company.Email ?? string.Empty;
        companySheet.Cell(4, 1).Value = "Adresse";
        companySheet.Cell(4, 2).Value = company.CompanyAddress ?? string.Empty;
        companySheet.Cell(5, 1).Value = "ICE Number";
        companySheet.Cell(5, 2).Value = company.IceNumber ?? string.Empty;

        var companyHeader = companySheet.Range(1, 1, 1, 2);
        companyHeader.Style.Font.Bold = true;
        companyHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        companyHeader.Style.Font.FontSize = 14;

        var companyDataRange = companySheet.Range(2, 1, 5, 2);
        companyDataRange.Style.Protection.SetLocked(true);
        companySheet.Columns().AdjustToContents();
        companySheet.Protect("payzen_societe_readonly");

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
            "Catégorie",
            "Salaire",
            "CNSS",
            "CIMR",
            "RIB",
            "Pays",
            "Ville",
        };

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        headerRange.Style.Font.FontSize = 16;
        headerRange.Style.Protection.SetLocked(true);

        var dataRange = ws.Range(2, 1, 500, headers.Length);
        dataRange.Style.Protection.SetLocked(false);

        ws.Column(7).Style.DateFormat.Format = "dd/MM/yyyy";
        ws.Column(14).Style.DateFormat.Format = "dd/MM/yyyy";
        ws.Columns().AdjustToContents();
        ws.Rows().AdjustToContents();
        ws.Protect("payzen_import_header_only");

        var listSheet = wb.Worksheets.Add("_lists");
        FillListColumn(listSheet, 1, "Departements", departments);
        FillListColumn(listSheet, 2, "Postes", jobPositions);
        FillListColumn(listSheet, 3, "TypesContrat", contractTypes);
        FillListColumn(listSheet, 4, "Managers", managers);
        FillListColumn(listSheet, 5, "Genres", genders);
        FillListColumn(listSheet, 6, "SituationsFamiliales", maritalStatuses);
        FillListColumn(listSheet, 7, "NiveauxEducation", educationLevels);
        FillListColumn(listSheet, 8, "Pays", countries.Select(c => c.CountryName).ToList());
        FillListColumn(listSheet, 9, "Categories", categories);
        FillListColumn(listSheet, 10, "PaysRangeKeys", countries.Select(c => ToExcelRangeName(c.CountryName)).ToList());
        FillListColumn(listSheet, 11, "Matricules", employeesChanges.Select(e => e.Matricule?.ToString() ?? string.Empty).Where(m => !string.IsNullOrWhiteSpace(m)).ToList());
        FillListColumn(listSheet, 12, "TypesAbsence", new List<string> { "Journée", "Demi-journée", "Heures" });
        FillListColumn(listSheet, 13, "OuiNon", new List<string> { "Oui", "Non" });
        FillListColumn(listSheet, 14, "TypesConge", leaveTypes);
        FillListColumn(listSheet, 15, "TypesHeuresSup", new List<string> { "Jour ouvrable", "Weekend", "Jour férié", "Nuit" });

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
        AddValidation(ws, "O2:O500", listSheet, 9, categories.Count);
        AddValidation(ws, "T2:T500", listSheet, 8, countries.Count);

        // Colonne d'aide masquée: mappe le pays choisi vers le nom de plage "cities_*".
        if (countries.Count > 0)
        {
            var lastCountryRow = countries.Count + 1;
            for (var row = 2; row <= 500; row++)
            {
                ws.Cell(row, 22).FormulaA1 =
                    $"IFERROR(INDEX('_lists'!$J$2:$J${lastCountryRow},MATCH($T{row},'_lists'!$H$2:$H${lastCountryRow},0)),\"\")";
            }
            ws.Column(22).Hide();
        }

        AddDependentCityValidation(ws, "U2:U500", "V");

        // Nouveaux employées logique fini ici
        // Pour chaque nouvelle feuille, c'est ici qu'on commence
        // Cette feuille est pour les changements d'employés

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
            "RIB",
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
        headerRangeChanges.Style.Protection.SetLocked(true);

        var changesDataRange = changesSheet.Range(2, 1, 500, changesHeaders.Length);
        changesDataRange.Style.Protection.SetLocked(false);

        var changesRow = 2;
        foreach (var employee in employeesChanges)
        {
            // Les lignes Excel sont 1-based : on écrit séquentiellement pour éviter
            // d'utiliser une valeur métier (matricule) comme numéro de ligne.
            changesSheet.Cell(changesRow, 1).Value = employee.Matricule?.ToString() ?? string.Empty;
            changesSheet.Cell(changesRow, 2).Value = employee.LastName ?? string.Empty;
            changesSheet.Cell(changesRow, 3).Value = employee.FirstName ?? string.Empty;
            changesRow++;
        }
        AddValidation(changesSheet, "A2:A500", listSheet, 11, employeesChanges.Count(e => !string.IsNullOrWhiteSpace(e.Matricule?.ToString())));
        AddValidation(changesSheet, "E2:E500", listSheet, 5, genders.Count);
        AddValidation(changesSheet, "N2:N500", listSheet, 8, countries.Count);
        AddValidation(changesSheet, "O2:O500", listSheet, 7, educationLevels.Count);
        AddValidation(changesSheet, "P2:P500", listSheet, 6, maritalStatuses.Count);
        AddValidation(changesSheet, "Q2:Q500", listSheet, 4, managers.Count);
        AddValidation(changesSheet, "R2:R500", listSheet, 2, jobPositions.Count);
        AddValidation(changesSheet, "S2:S500", listSheet, 3, contractTypes.Count);
        changesSheet.Protect("payzen_import_header_only");

        var absenceSheet = wb.Worksheets.Add("Absence");
        var absenceHeaders = new[]
        {
            "Matricule",
            "Nom",
            "Prénom",
            "Date absence",
            "Type durée",
            "Nombre d'heures",
            "Motif",
            "Justifiée (Oui/Non)"
        };
        for (var i = 0; i < absenceHeaders.Length; i++)
            absenceSheet.Cell(1, i + 1).Value = absenceHeaders[i];
        var absenceHeaderRange = absenceSheet.Range(1, 1, 1, absenceHeaders.Length);
        absenceHeaderRange.Style.Font.Bold = true;
        absenceHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        absenceHeaderRange.Style.Font.FontSize = 16;
        absenceHeaderRange.Style.Protection.SetLocked(true);
        absenceSheet.Range(2, 1, 500, absenceHeaders.Length).Style.Protection.SetLocked(false);
        absenceSheet.Column(4).Style.DateFormat.Format = "dd/MM/yyyy";
        absenceSheet.Column(6).Style.NumberFormat.Format = "0.00";
        AddValidation(absenceSheet, "A2:A500", listSheet, 11, employeesChanges.Count(e => !string.IsNullOrWhiteSpace(e.Matricule?.ToString())));
        AddValidation(absenceSheet, "E2:E500", listSheet, 12, 3);
        AddValidation(absenceSheet, "H2:H500", listSheet, 13, 2);
        absenceSheet.Columns().AdjustToContents();
        absenceSheet.Protect("payzen_import_header_only");

        var leaveSheet = wb.Worksheets.Add("Congé");
        var leaveHeaders = new[]
        {
            "Matricule",
            "Nom",
            "Prénom",
            "Date début",
            "Date fin",
            "Type de congé",
            "Commentaire"
        };
        for (var i = 0; i < leaveHeaders.Length; i++)
            leaveSheet.Cell(1, i + 1).Value = leaveHeaders[i];
        var leaveHeaderRange = leaveSheet.Range(1, 1, 1, leaveHeaders.Length);
        leaveHeaderRange.Style.Font.Bold = true;
        leaveHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        leaveHeaderRange.Style.Font.FontSize = 16;
        leaveHeaderRange.Style.Protection.SetLocked(true);
        leaveSheet.Range(2, 1, 500, leaveHeaders.Length).Style.Protection.SetLocked(false);
        leaveSheet.Column(4).Style.DateFormat.Format = "dd/MM/yyyy";
        leaveSheet.Column(5).Style.DateFormat.Format = "dd/MM/yyyy";
        AddValidation(leaveSheet, "A2:A500", listSheet, 11, employeesChanges.Count(e => !string.IsNullOrWhiteSpace(e.Matricule?.ToString())));
        AddValidation(leaveSheet, "F2:F500", listSheet, 14, leaveTypes.Count);
        leaveSheet.Columns().AdjustToContents();
        leaveSheet.Protect("payzen_import_header_only");

        var overtimeSheet = wb.Worksheets.Add("Heurs Sup");
        var overtimeHeaders = new[]
        {
            "Matricule",
            "Nom",
            "Prénom",
            "Date",
            "Nombre d'heures",
            "Type heure sup",
            "Commentaire"
        };
        for (var i = 0; i < overtimeHeaders.Length; i++)
            overtimeSheet.Cell(1, i + 1).Value = overtimeHeaders[i];
        var overtimeHeaderRange = overtimeSheet.Range(1, 1, 1, overtimeHeaders.Length);
        overtimeHeaderRange.Style.Font.Bold = true;
        overtimeHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        overtimeHeaderRange.Style.Font.FontSize = 16;
        overtimeHeaderRange.Style.Protection.SetLocked(true);
        overtimeSheet.Range(2, 1, 500, overtimeHeaders.Length).Style.Protection.SetLocked(false);
        overtimeSheet.Column(4).Style.DateFormat.Format = "dd/MM/yyyy";
        overtimeSheet.Column(5).Style.NumberFormat.Format = "0.00";
        AddValidation(overtimeSheet, "A2:A500", listSheet, 11, employeesChanges.Count(e => !string.IsNullOrWhiteSpace(e.Matricule?.ToString())));
        AddValidation(overtimeSheet, "F2:F500", listSheet, 15, 4);
        overtimeSheet.Columns().AdjustToContents();
        overtimeSheet.Protect("payzen_import_header_only");

        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        var fileName = $"import_changes_{Slugify(company.CompanyName)}_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        Console.WriteLine($"Template Name is : {fileName}");
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

    private static void AddDependentCityValidation(IXLWorksheet targetSheet, string rangeAddress, string helperColumnLetter)
    {
        var formula = $"INDIRECT(${helperColumnLetter}2)";
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
