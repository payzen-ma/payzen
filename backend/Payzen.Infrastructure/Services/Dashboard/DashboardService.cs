using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Dashboard;
using Payzen.Application.Interfaces;
using Payzen.Infrastructure.Persistence;
using Payzen.Domain.Enums;
using System.Text;

namespace Payzen.Infrastructure.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly IWorkingDaysCalculator _workingDays;
    public DashboardService(AppDbContext db, IWorkingDaysCalculator workingDays)
    {
        _db = db;
        _workingDays = workingDays;
    }

    public async Task<ServiceResult<CeoDashboardDto>> GetCeoDashboardDataAsync(
        int userId,
        string? parity = null,
        string? fromMonth = null,
        string? toMonth = null,
        CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct);

        if (user == null)
            return ServiceResult<CeoDashboardDto>.Fail("Utilisateur introuvable");

        int? companyId = user.Employee?.CompanyId;
        if (!companyId.HasValue)
            return ServiceResult<CeoDashboardDto>.Fail("Aucune société associée à l'utilisateur.");

        var (startMonth, endMonth) = ResolvePeriod(fromMonth, toMonth);
        var employeeQuery = _db.Employees
            .AsNoTracking()
            .Include(e => e.Gender)
            .Include(e => e.Departement)
            .Where(e => e.DeletedAt == null && e.CompanyId == companyId.Value);

        var parityNormalized = NormalizeParity(parity);
        if (parityNormalized != null)
        {
            employeeQuery = employeeQuery.Where(e => e.Gender != null && e.Gender.Code == parityNormalized);
        }

        var employees = await employeeQuery.ToListAsync(ct);
        var employeeIds = employees.Select(e => e.Id).ToList();

        // Masse salariale réelle: basée sur les résultats de paie (PayrollResults) en base.
        // Cela reflète ce qui est effectivement calculé (NetAPayer/TotalNet et charges patronales).
        var months = EnumerateMonths(startMonth, endMonth).ToList();
        var monthKeys = months.Select(m => new { m.Year, m.Month }).ToList();

        var payrollRows = await _db.PayrollResults
            .AsNoTracking()
            .Where(pr => pr.DeletedAt == null)
            .Where(pr => pr.CompanyId == companyId.Value)
            .Where(pr => pr.Status == PayrollResultStatus.OK)
            .Where(pr => employeeIds.Contains(pr.EmployeeId))
            .Where(pr => pr.Year > startMonth.Year || (pr.Year == startMonth.Year && pr.Month >= startMonth.Month))
            .Where(pr => pr.Year < endMonth.Year || (pr.Year == endMonth.Year && pr.Month <= endMonth.Month))
            .Select(pr => new
            {
                pr.Year,
                pr.Month,
                Net = pr.TotalNet ?? pr.NetAPayer ?? pr.TotalNet2 ?? 0m,
                Charges = pr.TotalCotisationsPatronales ?? 0m
            })
            .ToListAsync(ct);

        var grouped = payrollRows
            .GroupBy(x => (x.Year, x.Month))
            .ToDictionary(g => g.Key, g => new
            {
                Net = g.Sum(x => x.Net),
                Charges = g.Sum(x => x.Charges)
            });

        var chart = months.Select(m =>
        {
            grouped.TryGetValue((m.Year, m.Month), out var g);
            var netMad = g?.Net ?? 0m;
            var chargesMad = g?.Charges ?? 0m;
            return new CeoChartPointDto
            {
                Month = $"{m.Year:D4}-{m.Month:D2}",
                NetMad = netMad,
                ChargesMad = chargesMad
            };
        }).ToList();

        var totalEmployees = employees.Count;
        var women = employees.Count(e => e.Gender?.Code == "F");
        var men = employees.Count(e => e.Gender?.Code == "M");
        var parityPct = totalEmployees > 0 ? Math.Round((decimal)women / totalEmployees * 100m, 1) : 0m;

        var currentMonthPoint = chart.LastOrDefault();
        var previousMonthPoint = chart.Count > 1 ? chart[^2] : null;
        var netCurrent = currentMonthPoint?.NetMad ?? 0m;
        var netPrevious = previousMonthPoint?.NetMad ?? 0m;
        var netDeltaPct = netPrevious > 0m ? Math.Round(((netCurrent - netPrevious) / netPrevious) * 100m, 1) : 0m;

        var departments = employees
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Departement?.DepartementName) ? "Non affecté" : e.Departement!.DepartementName)
            .Select(g => new CeoDepartmentDto
            {
                Name = g.Key,
                Value = g.Count(),
                Percentage = totalEmployees > 0 ? Math.Round((decimal)g.Count() / totalEmployees * 100m, 1) : 0m,
                Color = ResolveDepartmentColor(g.Key)
            })
            .OrderByDescending(d => d.Value)
            .Take(6)
            .ToList();

        var dto = new CeoDashboardDto
        {
            Kpis = new List<CeoKpiDto>
            {
                new() { Title = "EFFECTIF TOTAL", Value = totalEmployees.ToString(), Subtitle = $"Période {months.First():MM/yyyy} - {months.Last():MM/yyyy}", SubtitleColor = "text-gray-500" },
                new() { Title = "PARITÉ H/F", Value = $"{women}/{men}", Subtitle = $"{parityPct}% femmes", SubtitleColor = "text-gray-500" },
                new() { Title = "MASSE SALARIALE", Value = $"{Math.Round(netCurrent, 0):N0} MAD", Subtitle = $"{(netDeltaPct >= 0 ? "+" : "")}{netDeltaPct}% vs mois précédent", SubtitleColor = netDeltaPct >= 0 ? "text-emerald-600" : "text-red-600" },
                new() { Title = "CHARGES PATR.", Value = $"{Math.Round((currentMonthPoint?.ChargesMad ?? 0m), 0):N0} MAD", Subtitle = "Total cotisations patronales", SubtitleColor = "text-gray-500" }
            },
            EvolutionChart = chart,
            Departments = departments,
            PayIndicators = new List<CeoPayIndicatorDto>
            {
                new() { Label = "Masse nette (période)", Value = $"{Math.Round(chart.Sum(c => c.NetMad), 0):N0} MAD" },
                new() { Label = "Charges patronales (période)", Value = $"{Math.Round(chart.Sum(c => c.ChargesMad), 0):N0} MAD" },
                new() { Label = "Filtre parité", Value = parityNormalized == null ? "Tous" : (parityNormalized == "F" ? "Femmes" : "Hommes") },
                new() { Label = "Période", Value = $"{months.First():MM/yyyy} - {months.Last():MM/yyyy}" }
            },
            Alerts = new List<CeoAlertDto>()
        };

        return ServiceResult<CeoDashboardDto>.Ok(dto);
    }

    private static string? NormalizeParity(string? parity)
    {
        if (string.IsNullOrWhiteSpace(parity))
            return null;
        var p = parity.Trim().ToUpperInvariant();
        if (p is "F" or "FEMME" or "FEMALE" or "W")
            return "F";
        if (p is "H" or "M" or "HOMME" or "MALE")
            return "M";
        return null;
    }

    private static (DateTime startMonth, DateTime endMonth) ResolvePeriod(string? fromMonth, string? toMonth)
    {
        var now = DateTime.UtcNow;
        var end = ParseMonth(toMonth) ?? new DateTime(now.Year, now.Month, 1);
        var start = ParseMonth(fromMonth) ?? end.AddMonths(-5);
        if (start > end)
        {
            (start, end) = (end, start);
        }

        return (new DateTime(start.Year, start.Month, 1), new DateTime(end.Year, end.Month, 1));
    }

    private static DateTime? ParseMonth(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateTime.TryParseExact(value + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? new DateTime(d.Year, d.Month, 1)
            : null;
    }

    private static IEnumerable<DateTime> EnumerateMonths(DateTime startMonth, DateTime endMonth)
    {
        var cursor = new DateTime(startMonth.Year, startMonth.Month, 1);
        var end = new DateTime(endMonth.Year, endMonth.Month, 1);
        while (cursor <= end)
        {
            yield return cursor;
            cursor = cursor.AddMonths(1);
        }
    }

    private static string ResolveDepartmentColor(string department)
    {
        var hash = Math.Abs(department.GetHashCode());
        var palette = new[]
        {
            "bg-emerald-500",
            "bg-blue-500",
            "bg-purple-500",
            "bg-amber-500",
            "bg-rose-500",
            "bg-cyan-500"
        };
        return palette[hash % palette.Length];
    }

    public async Task<DashboardHrRawDto> GetHrDashboardRawAsync(int? companyId, string? month, CancellationToken ct = default)
    {
        var empQ = _db.Employees
            .Include(e => e.Departement)
            .Include(e => e.Status)
            .Include(e => e.Gender)
            .AsQueryable();

        if (companyId.HasValue)
            empQ = empQ
                .Where(e => e.CompanyId == companyId.Value);

        var employees = await empQ.ToListAsync(ct);

        var empIds = employees.Select(e => e.Id).ToList();

        var contracts = await _db.EmployeeContracts
            .Where(c => empIds.Contains(c.EmployeeId))
            .Include(c => c.JobPosition).Include(c => c.ContractType)
            .ToListAsync(ct);

        var salaries = await _db.EmployeeSalaries
            .Where(s => empIds.Contains(s.EmployeeId))
            .ToListAsync(ct);

        var parsedMonth = month != null && DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out var d) ? d : DateOnly.FromDateTime(DateTime.Today);

        return new DashboardHrRawDto
        {
            Meta = new DashboardHrMetaDto
            {
                CompanyId = companyId ?? 0,
                Month = month ?? DateTime.Today.ToString("yyyy-MM"),
                GeneratedAt = DateTimeOffset.UtcNow
            },

            Employees = employees.Select(e => new DashboardHrRawEmployeeDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Department = e.Departement?.DepartementName ?? string.Empty,
                StatusCode = e.Status?.Code ?? string.Empty,
                GenderCode = e.Gender?.Code ?? string.Empty
            }).ToList(),

            Contracts = contracts.Select(c => new DashboardHrRawContractDto
            {
                EmployeeId = c.EmployeeId,
                StartDate = DateOnly.FromDateTime(c.StartDate),
                EndDate = c.EndDate.HasValue ? DateOnly.FromDateTime(c.EndDate.Value) : null,
                Position = c.JobPosition?.Name ?? string.Empty,
                ContractType = c.ContractType?.ContractTypeName ?? string.Empty
            }).ToList(),

            Salaries = salaries.Select(s => new DashboardHrRawSalaryDto
            {
                EmployeeId = s.EmployeeId,
                BaseSalary = s.BaseSalary ?? 0m,
                EffectiveDate = DateOnly.FromDateTime(s.EffectiveDate),
                EndDate = s.EndDate.HasValue ? DateOnly.FromDateTime(s.EndDate.Value) : null
            }).ToList()
        };
    }

    public async Task<DashboardHrDto> GetHrDashboardAsync(int? companyId, string? month, CancellationToken ct = default)
    {
        var raw = await GetHrDashboardRawAsync(companyId, month, ct);
        return new DashboardHrDto
        {
            Meta = raw.Meta,
            VueGlobale = await GetVueGlobaleAsync(companyId, month, ct),
            MouvementsRh = await GetMouvementsRhAsync(companyId, month, ct),
            MasseSalariale = await GetMasseSalarialeAsync(companyId, month, ct),
            PariteDiversite = await GetPariteDiversiteAsync(companyId, month, ct),
            ConformiteSociale = await GetConformiteSocialeAsync(companyId, month, ct)
        };
    }

    public async Task<DashboardHrVueGlobaleDto> GetVueGlobaleAsync(int? companyId, string? month, CancellationToken ct = default)
    {
        var q = _db.Employees.AsQueryable();
        if (companyId.HasValue)
            q = q.Where(e => e.CompanyId == companyId.Value);
        var total = await q.CountAsync(ct);
        var femalePct = total > 0 ? (decimal)await q.Where(e => e.Gender != null && e.Gender.Code == "F").CountAsync(ct) / total * 100 : 0;

        return new DashboardHrVueGlobaleDto
        {
            Kpis = new DashboardHrVueGlobaleKpisDto
            {
                EffectifTotal = total,
                Parite = new DashboardHrParityRatioDto { FemalePct = Math.Round(femalePct, 1), MalePct = Math.Round(100 - femalePct, 1) }
            }
        };
    }

    public async Task<DashboardHrMouvementsDto> GetMouvementsRhAsync(int? companyId, string? month, CancellationToken ct = default)
    {
        var parsedMonth = month != null && DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out var d) ? d : DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(parsedMonth.Year, parsedMonth.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var q = _db.EmployeeContracts.Include(c => c.Employee).ThenInclude(e => e!.Departement).Include(c => c.JobPosition).Include(c => c.ContractType).AsQueryable();
        if (companyId.HasValue)
            q = q.Where(c => c.CompanyId == companyId.Value);

        var allContracts = await q.ToListAsync(ct);
        var entrees = allContracts.Where(c => DateOnly.FromDateTime(c.StartDate) >= monthStart && DateOnly.FromDateTime(c.StartDate) <= monthEnd).ToList();
        var sorties = allContracts.Where(c => c.EndDate.HasValue && DateOnly.FromDateTime(c.EndDate.Value) >= monthStart && DateOnly.FromDateTime(c.EndDate.Value) <= monthEnd).ToList();

        var rows = entrees.Select(c => new DashboardHrMovementRowDto
        {
            EmployeeId = c.EmployeeId,
            EmployeeName = $"{c.Employee?.FirstName} {c.Employee?.LastName}",
            Department = c.Employee?.Departement?.DepartementName ?? string.Empty,
            Position = c.JobPosition?.Name ?? string.Empty,
            ContractType = c.ContractType?.ContractTypeName ?? string.Empty,
            Date = DateOnly.FromDateTime(c.StartDate),
            MovementType = Domain.Enums.Dashboard.DashboardHrMovementType.ENTRY
        }).Concat(sorties.Select(c => new DashboardHrMovementRowDto
        {
            EmployeeId = c.EmployeeId,
            EmployeeName = $"{c.Employee?.FirstName} {c.Employee?.LastName}",
            Department = c.Employee?.Departement?.DepartementName ?? string.Empty,
            Position = c.JobPosition?.Name ?? string.Empty,
            ContractType = c.ContractType?.ContractTypeName ?? string.Empty,
            Date = DateOnly.FromDateTime(c.EndDate!.Value),
            MovementType = Domain.Enums.Dashboard.DashboardHrMovementType.EXIT
        })).ToList();

        return new DashboardHrMouvementsDto
        {
            Summary = new DashboardHrMovementSummaryDto { Entrees = entrees.Count, Sorties = sorties.Count, SoldeNet = entrees.Count - sorties.Count },
            Rows = rows
        };
    }

    public async Task<DashboardHrMasseSalarialeDto> GetMasseSalarialeAsync(int? companyId, string? month, CancellationToken ct = default)
    {
        var q = _db.EmployeeSalaries.Include(s => s.Employee).ThenInclude(e => e!.Departement).AsQueryable();

        if (companyId.HasValue)
            q = q.Where(s => s.Employee.CompanyId == companyId.Value);

        var salaries = await q.ToListAsync(ct);

        var total = salaries.Sum(s => s.BaseSalary ?? 0m);

        return new DashboardHrMasseSalarialeDto
        {
            Kpis = new DashboardHrMasseSalarialeKpisDto { BrutTotalMad = total }
        };
    }

    public async Task<DashboardHrPariteDiversiteDto> GetPariteDiversiteAsync(int? companyId, string? month, CancellationToken ct = default)
    {
        var q = _db.Employees.Include(e => e.Gender).AsQueryable();
        if (companyId.HasValue)
            q = q.Where(e => e.CompanyId == companyId.Value);
        var total = await q.CountAsync(ct);
        var female = total > 0 ? await q.Where(e => e.Gender != null && e.Gender.Code == "F").CountAsync(ct) : 0;
        var male = total - female;

        return new DashboardHrPariteDiversiteDto
        {
            Kpis = new DashboardHrPariteDiversiteKpisDto
            {
                EffectifFemmes = female,
                EffectifHommes = male
            }
        };
    }

    public async Task<DashboardHrConformiteSocialeDto> GetConformiteSocialeAsync(int? companyId, string? month, CancellationToken ct = default)
    {
        return new DashboardHrConformiteSocialeDto
        {
            Kpis = new DashboardHrConformiteKpisDto()
        };
    }

    public async Task<ServiceResult<DashboardSummaryDto>> GetBackofficeSummaryAsync(CancellationToken ct = default)
    {
        var companies = await _db.Companies
            .AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .Select(c => new
            {
                c.Id,
                c.CompanyName,
                c.IsCabinetExpert,
                CountryName = c.Country != null ? c.Country.CountryName : null,
                CityName = c.City != null ? c.City.CityName : null,
                c.CreatedAt
            })
            .ToListAsync(ct);

        var totalCompanies = companies.Count;

        var empGroups = await _db.Employees
            .AsNoTracking()
            .Where(e => e.DeletedAt == null)
            .GroupBy(e => e.CompanyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var empDict = empGroups
            .ToDictionary(x => x.CompanyId, x => x.Count);

        var totalEmployees = empDict.Values.Sum();
        var accountingFirmsCount = companies.Count(c => c.IsCabinetExpert);
        var avgEmployeesPerCompany = totalCompanies > 0
            ? Math.Round((double)totalEmployees / totalCompanies, 2)
            : 0.0;

        var buckets = new Dictionary<string, (int companiesCount, int employeesCount)>
        {
            { "1-10", (0, 0) },
            { "11-50", (0, 0) },
            { "51-200", (0, 0) },
            { ">200", (0, 0) }
        };

        foreach (var company in companies)
        {
            var empCount = empDict.TryGetValue(company.Id, out var ctPerCompany) ? ctPerCompany : 0;

            var bucket = empCount <= 10
                ? "1-10"
                : empCount <= 50
                    ? "11-50"
                    : empCount <= 200
                        ? "51-200"
                        : ">200";

            var current = buckets[bucket];
            current.companiesCount += 1;
            current.employeesCount += empCount;
            buckets[bucket] = current;
        }

        var employeeDistribution = buckets
            .Select(b =>
            {
                var employeesCount = b.Value.employeesCount;
                var percentage = totalEmployees > 0
                    ? Math.Round((double)employeesCount / totalEmployees * 100, 1)
                    : 0.0;

                return new DistributionBucketDto
                {
                    Bucket = b.Key,
                    CompaniesCount = b.Value.companiesCount,
                    EmployeesCount = employeesCount,
                    Percentage = percentage
                };
            })
            .ToList();

        var recentCompanies = companies
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new RecentCompanyDto
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                CountryName = c.CountryName,
                CityName = c.CityName,
                EmployeesCount = empDict.TryGetValue(c.Id, out var ctPerCompany) ? ctPerCompany : 0,
                CreatedAt = c.CreatedAt
            })
            .ToList();

        var result = new DashboardSummaryDto
        {
            TotalCompanies = totalCompanies,
            TotalEmployees = totalEmployees,
            AccountingFirmsCount = accountingFirmsCount,
            AvgEmployeesPerCompany = avgEmployeesPerCompany,
            EmployeeDistribution = employeeDistribution,
            RecentCompanies = recentCompanies,
            AsOf = DateTimeOffset.UtcNow
        };

        return ServiceResult<DashboardSummaryDto>.Ok(result);
    }

    public async Task<ServiceResult<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default)
        => await GetBackofficeSummaryAsync(ct);

    public async Task<ServiceResult<object>> GetEmployeesSnapshotAsync(int? companyId, CancellationToken ct = default)
    {
        var q = _db.Employees.AsQueryable();
        if (companyId.HasValue)
            q = q.Where(e => e.CompanyId == companyId.Value);
        var count = await q.CountAsync(ct);
        return ServiceResult<object>.Ok(new
        {
            total = count
        });
    }

    public async Task<ServiceResult<ExpertDashboardDto>> GetExpertDashboardAsync(int expertCompanyId, CancellationToken ct = default)
    {
        var managed = await _db.Companies
            .AsNoTracking()
            .Where(c => c.DeletedAt == null && c.ManagedByCompanyId == expertCompanyId)
            .CountAsync(ct);
        return ServiceResult<ExpertDashboardDto>.Ok(new ExpertDashboardDto
        {
            ExpertCompanyId = expertCompanyId,
            TotalClients = managed,
            TotalEmployees = await _db.Employees.AsNoTracking().Where(e => e.DeletedAt == null && _db.Companies.Any(c => c.Id == e.CompanyId && c.ManagedByCompanyId == expertCompanyId && c.DeletedAt == null)).CountAsync(ct),
            AsOf = DateTimeOffset.UtcNow
        });
    }

    public async Task<ServiceResult<DashboardResponseDto>> GetEmployeesDashboardAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.DeletedAt == null, ct);

        if (user == null)
            return ServiceResult<DashboardResponseDto>.Fail("Utilisateur non trouvé");

        if (user.Employee == null)
            return ServiceResult<DashboardResponseDto>.Fail("L'utilisateur n'est pas associé à un employé");

        var companyId = user.Employee.CompanyId;

        var totalEmployees = await _db.Employees
            .Where(e => e.DeletedAt == null && e.CompanyId == companyId)
            .CountAsync(ct);

        var activeEmployees = await _db.Employees
            .Where(e => e.DeletedAt == null && e.CompanyId == companyId && e.Status != null && e.Status.Code.ToLower() == "active")
            .CountAsync(ct);

        var departements = await _db.Departements
            .Where(d => d.DeletedAt == null && d.CompanyId == companyId)
            .Select(d => d.DepartementName)
            .ToListAsync(ct);

        var statuses = await _db.Statuses
            .Select(s => s.Code)
            .ToListAsync(ct);

        var employees = await _db.Employees
            .AsNoTracking()
            .AsSplitQuery()
            .Where(e => e.DeletedAt == null && e.CompanyId == companyId)
            .Include(e => e.Company)
            .Include(e => e.Departement)
            .Include(e => e.Status)
            .Include(e => e.Manager)
            .Include(e => e.Documents)
            .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                .ThenInclude(c => c.JobPosition)
            .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                .ThenInclude(c => c.ContractType)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Select(e => new EmployeeDashboardItemDto
            {
                Id = e.Id.ToString(),
                FirstName = e.FirstName,
                LastName = e.LastName,
                Position = e.Contracts != null
                    ? e.Contracts
                        .Where(c => c.DeletedAt == null && c.EndDate == null)
                        .OrderByDescending(c => c.StartDate)
                        .Select(c => c.JobPosition!.Name)
                        .FirstOrDefault() ?? "Non assigné"
                    : "Non assigné",
                Department = e.Departement != null ? e.Departement.DepartementName : "Non assigné",
                statuses = e.Status != null ? e.Status.Code : string.Empty,
                NameFr = e.Status != null ? e.Status.NameFr : string.Empty,
                NameAr = e.Status != null ? e.Status.NameAr : string.Empty,
                NameEn = e.Status != null ? e.Status.NameEn : string.Empty,
                StartDate = e.Contracts != null
                    ? e.Contracts
                        .Where(c => c.DeletedAt == null && c.EndDate == null)
                        .OrderByDescending(c => c.StartDate)
                        .Select(c => c.StartDate.ToString("yyyy-MM-dd"))
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                MissingDocuments = e.Documents != null
                    ? e.Documents.Count(d => d.DeletedAt == null && string.IsNullOrEmpty(d.FilePath))
                    : 0,
                ContractType = e.Contracts != null
                    ? e.Contracts
                        .Where(c => c.DeletedAt == null && c.EndDate == null)
                        .OrderByDescending(c => c.StartDate)
                        .Select(c => c.ContractType!.ContractTypeName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                Manager = e.Manager != null
                    ? $"{e.Manager.FirstName} {e.Manager.LastName}"
                    : null,
                UserId = _db.Users
                    .Where(u => u.EmployeeId == e.Id && u.DeletedAt == null)
                    .Select(u => (int?)u.Id)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var response = new DashboardResponseDto
        {
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            Employees = employees,
            Departements = departements,
            statuses = statuses
        };

        return ServiceResult<DashboardResponseDto>.Ok(response);
    }

    public async Task<ServiceResult<EmployeeDashboardDataDto>> GetEmployeeDashboardAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Employee)
                .ThenInclude(e => e.Departement)
            .Include(u => u.Employee)
                .ThenInclude(e => e.Manager)
            .Include(u => u.Employee)
                .ThenInclude(e => e.Documents)
            .Include(u => u.Employee)
                .ThenInclude(e => e.Contracts!)
                    .ThenInclude(c => c.JobPosition)
            .Include(u => u.Employee)
                .ThenInclude(e => e.Contracts!)
                    .ThenInclude(c => c.ContractType)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.DeletedAt == null, ct);

        if (user == null)
            return ServiceResult<EmployeeDashboardDataDto>.Fail("Utilisateur non trouvé");

        if (user.Employee == null)
            return ServiceResult<EmployeeDashboardDataDto>.Fail("L'utilisateur n'est pas associé à un employé");

        var employee = user.Employee;

        var activeContract = employee.Contracts
            .Where(c => c.DeletedAt == null && c.EndDate == null)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefault();

        var firstName = (employee.FirstName ?? string.Empty).Trim();
        var lastName = (employee.LastName ?? string.Empty).Trim();
        var employeeName = $"{firstName} {lastName}".Trim();

        var initials = string.Empty;
        if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            initials = string.Concat(firstName[0], lastName[0]).ToUpperInvariant();
        else if (!string.IsNullOrWhiteSpace(firstName))
            initials = firstName[..1].ToUpperInvariant();
        else if (!string.IsNullOrWhiteSpace(lastName))
            initials = lastName[..1].ToUpperInvariant();

        // Rôle côté back-office : on lit le premier rôle actif.
        var roleName = await _db.UsersRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id && ur.DeletedAt == null)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .FirstOrDefaultAsync(ct);

        roleName ??= "Employee";

        var department = employee.Departement?.DepartementName ?? string.Empty;
        var contractType = activeContract?.ContractType?.ContractTypeName ?? string.Empty;
        var manager = employee.Manager != null
            ? $"{employee.Manager.FirstName} {employee.Manager.LastName}".Trim()
            : string.Empty;

        var matricule = employee.Matricule?.ToString() ?? string.Empty;

        // Ancienneté (MVP) : calcul depuis le startDate du contrat actif.
        var seniority = string.Empty;
        if (activeContract != null)
        {
            var start = DateOnly.FromDateTime(activeContract.StartDate);
            var nowDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var totalMonths = (nowDate.Year - start.Year) * 12 + (nowDate.Month - start.Month);
            totalMonths = Math.Max(0, totalMonths);

            var years = totalMonths / 12;
            var months = totalMonths % 12;
            seniority = years > 0 ? $"{years} ans" : $"{months} mois";
        }

        // Documents (UI attend "À venir" / "Expiré")
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var documents = employee.Documents
            .Where(d => d.DeletedAt == null)
            .Select(d =>
            {
                var exp = d.ExpirationDate.HasValue
                    ? DateOnly.FromDateTime(d.ExpirationDate.Value)
                    : (DateOnly?)null;

                var status = !exp.HasValue || exp.Value >= today ? "À venir" : "Expiré";

                return new EmployeeDocumentDto
                {
                    Title = d.Name,
                    Subtitle = d.DocumentType,
                    Status = status
                };
            })
            .ToList();

        var contractInfo = new List<ContractInfoDto>();
        if (activeContract != null)
        {
            contractInfo.Add(new ContractInfoDto
            {
                Label = "Poste",
                Value = activeContract.JobPosition?.Name ?? "Non assigné",
                IsTag = false
            });

            contractInfo.Add(new ContractInfoDto
            {
                Label = "Type contrat",
                Value = contractType,
                IsTag = true
            });
        }

        // KPI - calcul via données existantes (PayrollResult / LeaveBalances / Attendance / Overtime)
        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var currentMonthLabel = new DateTime(year, month, 1).ToString("MMMM yyyy", fr);

        // 1) Payroll (Net à payer)
        var payroll = await _db.PayrollResults
            .AsNoTracking()
            .Where(pr =>
                pr.EmployeeId == employee.Id &&
                pr.DeletedAt == null &&
                pr.Year == year &&
                pr.Month == month &&
                pr.Status == PayrollResultStatus.OK)
            .OrderByDescending(pr => pr.Id)
            .FirstOrDefaultAsync(ct);

        payroll ??= await _db.PayrollResults
            .AsNoTracking()
            .Where(pr =>
                pr.EmployeeId == employee.Id &&
                pr.DeletedAt == null &&
                pr.Status == PayrollResultStatus.OK)
            .OrderByDescending(pr => pr.Year)
            .ThenByDescending(pr => pr.Month)
            .ThenByDescending(pr => pr.Id)
            .FirstOrDefaultAsync(ct);

        var salaryNet = payroll?.NetAPayer ?? payroll?.TotalNet ?? payroll?.TotalNet2 ?? 0m;
        var paidDate = payroll != null
            ? new DateTime(payroll.Year, payroll.Month, 1).ToString("MMMM yyyy", fr)
            : string.Empty;

        // 2) Congés (solde annuel)
        var annualLeaveType = await _db.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(lt => lt.LeaveCode == "ANNUAL" && lt.DeletedAt == null, ct);

        decimal leavesRemainingDec = 0m;
        decimal leavesTotalDec = 0m;
        LeaveDetailDto? annualLeaveDetail = null;

        if (annualLeaveType != null)
        {
            var lb = await _db.LeaveBalances
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employee.Id &&
                    x.CompanyId == employee.CompanyId &&
                    x.LeaveTypeId == annualLeaveType.Id &&
                    x.DeletedAt == null &&
                    x.Year == year &&
                    x.Month == month, ct);

            lb ??= await _db.LeaveBalances
                .AsNoTracking()
                .Where(x =>
                    x.EmployeeId == employee.Id &&
                    x.CompanyId == employee.CompanyId &&
                    x.LeaveTypeId == annualLeaveType.Id &&
                    x.DeletedAt == null)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (lb != null)
            {
                // Remaining = closing; Total = opening + accrued + carry-in
                leavesRemainingDec = lb.ClosingDays;
                leavesTotalDec = lb.OpeningDays + lb.AccruedDays + lb.CarryInDays;

                var leavesRemaining = Math.Max(0, leavesRemainingDec);
                var leavesTotal = Math.Max(0, leavesTotalDec);

                annualLeaveDetail = new LeaveDetailDto
                {
                    Label = "Solde annuel",
                    Remaining = leavesRemaining,
                    Total = leavesTotal,
                    ColorClass = "bg-success",
                    IsText = leavesTotal <= 0,
                    Text = leavesTotal <= 0
                        ? $"{leavesRemaining:0.##} j"
                        : null
                };
            }
        }

        var leavesRemainingValue = Math.Max(0, leavesRemainingDec);
        var leavesTotalValue = Math.Max(0, leavesTotalDec);

        // 3) Présences = jours ouvrés - absences approuvées (1 jour / 0.5 jour) jusqu'aujourd'hui sur le mois en cours.
        var todayDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var workingDaysMonth = await _workingDays.CalculateWorkingDaysAsync(employee.CompanyId, monthStart, todayDate, ct);

        var absencesApproved = await _db.EmployeeAbsences
            .AsNoTracking()
            .Where(a =>
                a.DeletedAt == null &&
                a.EmployeeId == employee.Id &&
                a.AbsenceDate >= monthStart &&
                a.AbsenceDate <= todayDate &&
                a.Status == AbsenceStatus.Approved)
            .Select(a => a.DurationType)
            .ToListAsync(ct);

        var absencesDays = absencesApproved.Sum(dt => dt == AbsenceDurationType.HalfDay ? 0.5m : 1.0m);

        // PresenceDays = max(0, workingDays - absences)
        var presenceDaysDec = Math.Max(0m, workingDaysMonth - absencesDays);
        var presenceTotalDec = Math.Max(0m, workingDaysMonth);

        // Front attend des int pour l'instant => arrondi au 0.5 n'existe pas sur ce KPI.
        // On choisit une représentation stable: arrondi à l'entier inférieur pour "jours" affichés.
        var presenceDays = (int)Math.Floor(presenceDaysDec);
        var presenceTotal = (int)Math.Floor(presenceTotalDec);

        // 4) Heures sup en attente validation (Submitted)
        var overtimeRows = await _db.EmployeeOvertimes
            .AsNoTracking()
            .Where(o =>
                o.EmployeeId == employee.Id &&
                o.DeletedAt == null &&
                o.OvertimeDate >= monthStart &&
                o.OvertimeDate <= monthEnd &&
                o.Status == OvertimeStatus.Submitted)
            .ToListAsync(ct);

        var extraHours = overtimeRows.Sum(o => o.DurationInHours);

        // 5) Payslip details (minimaux, pour éviter un panneau vide)
        var payslipDetails = new List<PayslipDetailDto>();
        if (payroll != null)
        {
            if (payroll.TotalBrut.HasValue)
            {
                payslipDetails.Add(new PayslipDetailDto
                {
                    Label = "Total brut",
                    Value = $"{payroll.TotalBrut.Value:0} MAD",
                    Type = "normal"
                });
            }

            if (payroll.TotalCotisationsSalariales.HasValue)
            {
                payslipDetails.Add(new PayslipDetailDto
                {
                    Label = "Total cotisations salariales",
                    Value = $"{payroll.TotalCotisationsSalariales.Value:0} MAD",
                    Type = "deduction"
                });
            }
        }

        var dto = new EmployeeDashboardDataDto
        {
            EmployeeId = employee.Id,
            EmployeeName = employeeName,
            Initials = initials,
            Role = roleName,
            Department = department,
            ContractType = contractType,
            Matricule = matricule,
            Manager = manager,
            Seniority = seniority,

            SalaryNet = salaryNet,
            PaidDate = paidDate,

            LeavesRemaining = leavesRemainingValue,
            LeavesTotal = leavesTotalValue,

            PresenceDays = presenceDays,
            PresenceTotal = presenceTotal,

            ExtraHours = extraHours,

            LeavesDetails = annualLeaveDetail != null
                ? new List<LeaveDetailDto> { annualLeaveDetail }
                : new List<LeaveDetailDto>(),
            ContractInfo = contractInfo,
            PayslipDetails = payslipDetails,
            Documents = documents
        };

        return ServiceResult<EmployeeDashboardDataDto>.Ok(dto);
    }
}
