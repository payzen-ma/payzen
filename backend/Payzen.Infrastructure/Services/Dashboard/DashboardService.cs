using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Dashboard;
using Payzen.Application.Interfaces;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardHrRawDto> GetHrDashboardRawAsync(int? companyId, string? month, CancellationToken ct = default)
    {
        var empQ = _db.Employees.Include(e => e.Departement).Include(e => e.Status).Include(e => e.Gender).AsQueryable();
        if (companyId.HasValue) empQ = empQ.Where(e => e.CompanyId == companyId.Value);
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
            Meta = new DashboardHrMetaDto { CompanyId = companyId ?? 0, Month = month ?? DateTime.Today.ToString("yyyy-MM"), GeneratedAt = DateTimeOffset.UtcNow },
            Employees = employees.Select(e => new DashboardHrRawEmployeeDto { Id = e.Id, FirstName = e.FirstName, LastName = e.LastName, Department = e.Departement?.DepartementName ?? string.Empty, StatusCode = e.Status?.Code ?? string.Empty, GenderCode = e.Gender?.Code ?? string.Empty }).ToList(),
            Contracts = contracts.Select(c => new DashboardHrRawContractDto { EmployeeId = c.EmployeeId, StartDate = DateOnly.FromDateTime(c.StartDate), EndDate = c.EndDate.HasValue ? DateOnly.FromDateTime(c.EndDate.Value) : null, Position = c.JobPosition?.Name ?? string.Empty, ContractType = c.ContractType?.ContractTypeName ?? string.Empty }).ToList(),
            Salaries  = salaries.Select(s => new DashboardHrRawSalaryDto { EmployeeId = s.EmployeeId, BaseSalary = s.BaseSalary ?? 0m, EffectiveDate = DateOnly.FromDateTime(s.EffectiveDate), EndDate = s.EndDate.HasValue ? DateOnly.FromDateTime(s.EndDate.Value) : null }).ToList()
        };
    }

    public async Task<DashboardHrDto> GetHrDashboardAsync(int? companyId, string? month, CancellationToken ct = default)
    {
        var raw = await GetHrDashboardRawAsync(companyId, month, ct);
        return new DashboardHrDto
        {
            Meta             = raw.Meta,
            VueGlobale       = await GetVueGlobaleAsync(companyId, month, ct),
            MouvementsRh     = await GetMouvementsRhAsync(companyId, month, ct),
            MasseSalariale   = await GetMasseSalarialeAsync(companyId, month, ct),
            PariteDiversite  = await GetPariteDiversiteAsync(companyId, month, ct),
            ConformiteSociale= await GetConformiteSocialeAsync(companyId, month, ct)
        };
    }

    public async Task<DashboardHrVueGlobaleDto> GetVueGlobaleAsync(int? companyId, string? month, CancellationToken ct = default)
    {
        var q = _db.Employees.AsQueryable();
        if (companyId.HasValue) q = q.Where(e => e.CompanyId == companyId.Value);
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
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        var q = _db.EmployeeContracts.Include(c => c.Employee).ThenInclude(e => e!.Departement).Include(c => c.JobPosition).Include(c => c.ContractType).AsQueryable();
        if (companyId.HasValue) q = q.Where(c => c.CompanyId == companyId.Value);

        var allContracts = await q.ToListAsync(ct);
        var entrees = allContracts.Where(c => DateOnly.FromDateTime(c.StartDate) >= monthStart && DateOnly.FromDateTime(c.StartDate) <= monthEnd).ToList();
        var sorties  = allContracts.Where(c => c.EndDate.HasValue && DateOnly.FromDateTime(c.EndDate.Value) >= monthStart && DateOnly.FromDateTime(c.EndDate.Value) <= monthEnd).ToList();

        var rows = entrees.Select(c => new DashboardHrMovementRowDto
        {
            EmployeeId = c.EmployeeId, EmployeeName = $"{c.Employee?.FirstName} {c.Employee?.LastName}",
            Department = c.Employee?.Departement?.DepartementName ?? string.Empty,
            Position = c.JobPosition?.Name ?? string.Empty, ContractType = c.ContractType?.ContractTypeName ?? string.Empty,
            Date = DateOnly.FromDateTime(c.StartDate), MovementType = Domain.Enums.Dashboard.DashboardHrMovementType.ENTRY
        }).Concat(sorties.Select(c => new DashboardHrMovementRowDto
        {
            EmployeeId = c.EmployeeId, EmployeeName = $"{c.Employee?.FirstName} {c.Employee?.LastName}",
            Department = c.Employee?.Departement?.DepartementName ?? string.Empty,
            Position = c.JobPosition?.Name ?? string.Empty, ContractType = c.ContractType?.ContractTypeName ?? string.Empty,
            Date = DateOnly.FromDateTime(c.EndDate!.Value), MovementType = Domain.Enums.Dashboard.DashboardHrMovementType.EXIT
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
        if (companyId.HasValue) q = q.Where(s => s.Employee.CompanyId == companyId.Value);
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
        if (companyId.HasValue) q = q.Where(e => e.CompanyId == companyId.Value);
        var total  = await q.CountAsync(ct);
        var female = total > 0 ? await q.Where(e => e.Gender != null && e.Gender.Code == "F").CountAsync(ct) : 0;
        var male   = total - female;

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
        if (companyId.HasValue) q = q.Where(e => e.CompanyId == companyId.Value);
        var count = await q.CountAsync(ct);
        return ServiceResult<object>.Ok(new { total = count });
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
            TotalClients    = managed,
            TotalEmployees  = await _db.Employees.AsNoTracking().Where(e => e.DeletedAt == null && _db.Companies.Any(c => c.Id == e.CompanyId && c.ManagedByCompanyId == expertCompanyId && c.DeletedAt == null)).CountAsync(ct),
            AsOf            = DateTimeOffset.UtcNow
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
            var now = DateOnly.FromDateTime(DateTime.UtcNow);

            var totalMonths = (now.Year - start.Year) * 12 + (now.Month - start.Month);
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

        // KPI MVP (pour que le front fonctionne) : initialement à 0/vides.
        var dto = new EmployeeDashboardDataDto
        {
            EmployeeName = employeeName,
            Initials = initials,
            Role = roleName,
            Department = department,
            ContractType = contractType,
            Matricule = matricule,
            Manager = manager,
            Seniority = seniority,

            SalaryNet = 0m,
            PaidDate = string.Empty,

            LeavesRemaining = 0,
            LeavesTotal = 0,

            PresenceDays = 0,
            PresenceTotal = 0,

            ExtraHours = 0m,

            LeavesDetails = new List<LeaveDetailDto>(),
            ContractInfo = contractInfo,
            PayslipDetails = new List<PayslipDetailDto>(),
            Documents = documents
        };

        return ServiceResult<EmployeeDashboardDataDto>.Ok(dto);
    }
}
