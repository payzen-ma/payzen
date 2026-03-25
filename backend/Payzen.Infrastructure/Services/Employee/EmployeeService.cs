using Microsoft.AspNetCore.Hosting;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Auth;
using Payzen.Application.DTOs.Dashboard;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Auth;
using Payzen.Domain.Entities.Employee;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Payzen.Infrastructure.Services.Employee;

/// <summary>
/// Façade unique qui implémente IEmployeeService en déléguant aux sous-services spécialisés.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _db;
    private readonly IEmployeeEventLogService _eventLog;
    private readonly IInvitationService _invitationService;
    private readonly ILogger<EmployeeService> _logger;
    private readonly EmployeeContractService   _contracts;
    private readonly EmployeeSalaryService     _salaries;
    private readonly EmployeeDocumentService   _documents;
    private readonly EmployeeAddressService    _addresses;
    private readonly EmployeeFamilyService     _family;
    private readonly EmployeeAbsenceService    _absences;
    private readonly EmployeeOvertimeService   _overtimes;
    private readonly EmployeeAttendanceService _attendances;

    public EmployeeService(
        AppDbContext db,
        IWebHostEnvironment env,
        IEmployeeEventLogService eventLog,
        IInvitationService invitationService,
        ILogger<EmployeeService> logger)
    {
        _db          = db;
        _eventLog    = eventLog;
        _invitationService = invitationService;
        _logger      = logger;
        _contracts   = new EmployeeContractService(db, eventLog);
        _salaries    = new EmployeeSalaryService(db, eventLog);
        _documents   = new EmployeeDocumentService(db, env);
        _addresses   = new EmployeeAddressService(db, eventLog);
        _family      = new EmployeeFamilyService(db);
        _absences    = new EmployeeAbsenceService(db);
        _overtimes   = new EmployeeOvertimeService(db);
        _attendances = new EmployeeAttendanceService(db);
    }

    // ── Employee (core) ──────────────────────────────────────────────────────

    public async Task<ServiceResult<DashboardResponseDto>> GetAllAsync(int? companyId, CancellationToken ct = default)
    {
        var q = _db.Employees.AsQueryable();
        if (companyId.HasValue) q = q.Where(e => e.CompanyId == companyId.Value);
        q = q.Where(e => e.DeletedAt == null);

        var employees = await q
            .AsNoTracking()
            .Include(e => e.Departement)
            .Include(e => e.Status)
            .Include(e => e.Manager)
            .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                .ThenInclude(c => c.JobPosition)
            .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                .ThenInclude(c => c.ContractType)
            .OrderBy(e => e.LastName)
            .ToListAsync(ct);

        var empIds = employees.Select(e => e.Id).ToList();
        var userRows = await _db.Users.AsNoTracking()
            .Where(u => u.EmployeeId != null && empIds.Contains(u.EmployeeId.Value) && u.DeletedAt == null)
            .Select(u => new { EmpId = u.EmployeeId!.Value, u.Id })
            .ToListAsync(ct);
        var userByEmployeeId = userRows
            .GroupBy(x => x.EmpId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var items = new List<EmployeeDashboardItemDto>(employees.Count);
        foreach (var e in employees)
        {
            var activeContract = e.Contracts?
                .Where(c => c.DeletedAt == null && c.EndDate == null)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefault();

            int? userId = userByEmployeeId.TryGetValue(e.Id, out var uid) ? uid : null;

            items.Add(new EmployeeDashboardItemDto
            {
                Id = e.Id.ToString(),
                FirstName = e.FirstName,
                LastName = e.LastName,
                Position = activeContract?.JobPosition?.Name ?? string.Empty,
                Department = e.Departement?.DepartementName ?? string.Empty,
                // Code métier (ACTIVE, …) pour le filtre / mapEmployeeStatus côté Angular
                statuses = e.Status?.Code ?? string.Empty,
                NameFr = e.Status?.NameFr ?? string.Empty,
                NameEn = e.Status?.NameEn ?? string.Empty,
                NameAr = e.Status?.NameAr ?? string.Empty,
                StartDate = activeContract != null
                    ? activeContract.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : string.Empty,
                MissingDocuments = 0,
                ContractType = activeContract?.ContractType?.ContractTypeName ?? string.Empty,
                Manager = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}".Trim() : null,
                UserId = userId
            });
        }

        var activeCount = employees.Count(e => e.Status?.IsActive == true);
        var departements = items
            .Select(i => i.Department)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ServiceResult<DashboardResponseDto>.Ok(new DashboardResponseDto
        {
            TotalEmployees = items.Count,
            ActiveEmployees = activeCount,
            Employees = items,
            Departements = departements
        });
    }

    public async Task<ServiceResult<IEnumerable<EmployeeReadDto>>> GetAllSimpleAsync(CancellationToken ct = default)
    {
        var list = await _db.Employees.OrderBy(e => e.LastName).Select(e => MapToRead(e)).ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<EmployeeReadDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.Employees
            .Include(e => e.Company)
            .Include(e => e.Departement)
            .Include(e => e.Status)
            .Include(e => e.Manager)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        return e == null ? ServiceResult<EmployeeReadDto>.Fail("Employé introuvable.") : ServiceResult<EmployeeReadDto>.Ok(MapToRead(e));
    }

    public async Task<ServiceResult<EmployeeDetailDto>> GetDetailAsync(int id, int requestingUserId, CancellationToken ct = default)
    {
        var canViewSensitiveDetails = await _db.UsersRoles.AsNoTracking()
            .Where(ur => ur.UserId == requestingUserId && ur.DeletedAt == null)
            .Join(_db.Roles.AsNoTracking().Where(r => r.DeletedAt == null),
                ur => ur.RoleId, r => r.Id, (_, r) => r)
            .AnyAsync(r => r.Name.ToLower() == "rh", ct);

        var e = await _db.Employees
            .AsNoTracking()
            .Where(emp => emp.Id == id && emp.DeletedAt == null)
            .Include(x => x.Company)
            .Include(x => x.Departement)
            .Include(x => x.Status)
            .Include(x => x.Gender)
            .Include(x => x.Nationality)
            .Include(x => x.MaritalStatus)
            .Include(x => x.Category)
            .Include(x => x.Manager)
            .Include(x => x.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                .ThenInclude(c => c.JobPosition)
            .Include(x => x.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                .ThenInclude(c => c.ContractType)
            .Include(x => x.Salaries!.Where(s => s.DeletedAt == null && s.EndDate == null))
                .ThenInclude(s => s.Components!.Where(c => c.DeletedAt == null && c.EndDate == null))
            .Include(x => x.Addresses!.Where(a => a.DeletedAt == null))
                .ThenInclude(a => a.City)
                .ThenInclude(c => c!.Country)
            .FirstOrDefaultAsync(ct);

        if (e == null) return ServiceResult<EmployeeDetailDto>.Fail("Employé introuvable.");

        var primaryAddr = e.Addresses
            .Where(a => a.DeletedAt == null)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefault();

        var contracts = e.Contracts.Where(c => c.DeletedAt == null).ToList();
        var activeContract = contracts
            .Where(c => c.EndDate == null)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefault();

        var salaries = e.Salaries.Where(s => s.DeletedAt == null).ToList();
        var activeSalary = salaries
            .Where(s => s.EndDate == null)
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefault();

        var salaryComponents = activeSalary?.Components?
            .Where(c => c.DeletedAt == null && c.EndDate == null)
            .Select(c => new SalaryComponentDto
            {
                ComponentName = c.ComponentType,
                Amount = c.Amount,
                IsTaxable = c.IsTaxable
            })
            .ToList() ?? new List<SalaryComponentDto>();

        var baseSalary = activeSalary?.BaseSalary ?? 0;
        var totalComponents = salaryComponents.Sum(x => x.Amount);

        var eventLogs = await _db.EmployeeEventLogs.AsNoTracking()
            .Where(l => l.employeeId == id && l.DeletedAt == null)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        var modifierIds = eventLogs.Select(l => l.CreatedBy).Distinct().ToList();
        var modifiers = await LoadEmployeeLogModifiersAsync(modifierIds, ct);
        var formattedEvents = EmployeeDetailHistoryFormatter.Build(eventLogs, canViewSensitiveDetails, modifiers);

        return ServiceResult<EmployeeDetailDto>.Ok(new EmployeeDetailDto
        {
            Id = e.Id, FirstName = e.FirstName, LastName = e.LastName,
            CinNumber = e.CinNumber, DateOfBirth = e.DateOfBirth,
            Email = e.Email, Phone = e.Phone,
            CompanyId = e.CompanyId,
            CountryPhoneCode = primaryAddr?.City?.Country?.CountryPhoneCode,
            departments = e.Departement?.DepartementName,
            StatusName = e.Status?.Code,
            GenderId = e.GenderId,
            GenderName = e.Gender?.Code,
            MaritalStatusName = e.MaritalStatus?.Code,
            CategoryId = e.CategoryId,
            CategoryName = e.Category?.Name,
            ManagerName = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}".Trim() : null,
            JobPositionId = activeContract?.JobPositionId,
            JobPositionName = activeContract?.JobPosition?.Name,
            ContractTypeId = activeContract?.ContractTypeId,
            ContractTypeName = activeContract?.ContractType?.ContractTypeName,
            ContractStartDate = activeContract?.StartDate,
            DepartementId = e.DepartementId,
            BaseSalary = activeSalary?.BaseSalary,
            BaseSalaryHourly = activeSalary?.BaseSalaryHourly,
            SalaryEffectiveDate = activeSalary?.EffectiveDate,
            SalaryComponents = salaryComponents,
            TotalSalary = baseSalary + totalComponents,
            SalaryPaymentMethod = e.PaymentMethod,
            Address = primaryAddr == null
                ? null
                : new EmployeeAddressDto
                {
                    AddressLine1 = primaryAddr.AddressLine1,
                    AddressLine2 = primaryAddr.AddressLine2,
                    ZipCode = primaryAddr.ZipCode,
                    CityId = primaryAddr.CityId,
                    CityName = primaryAddr.City?.CityName ?? string.Empty,
                    CountryId = primaryAddr.City?.CountryId,
                    CountryName = primaryAddr.City?.Country?.CountryName ?? string.Empty
                },
            cnss = e.CnssNumber, cimr = e.CimrNumber,
            cimrEmployeeRate = e.CimrEmployeeRate,
            cimrCompanyRate = e.CimrCompanyRate,
            hasPrivateInsurance = e.HasPrivateInsurance,
            privateInsuranceNumber = e.PrivateInsuranceNumber,
            privateInsuranceRate = e.PrivateInsuranceRate,
            disableAmo = e.DisableAmo,
            Events = formattedEvents,
            CreatedAt = e.CreatedAt.DateTime
        });
    }

    private async Task<Dictionary<int, (string Name, string Role)>> LoadEmployeeLogModifiersAsync(
        IReadOnlyCollection<int> userIds, CancellationToken ct)
    {
        var dict = new Dictionary<int, (string Name, string Role)>();
        if (userIds.Count == 0) return dict;

        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Include(u => u.Employee)
            .Include(u => u.UsersRoles!.Where(ur => ur.DeletedAt == null))
            .ThenInclude(ur => ur.Role)
            .ToListAsync(ct);

        foreach (var u in users)
        {
            var name = u.Employee != null
                ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim()
                : "Système";
            if (string.IsNullOrWhiteSpace(name)) name = "Système";

            var roleName = u.UsersRoles?
                .Where(ur => ur.DeletedAt == null)
                .Select(ur => ur.Role?.Name)
                .FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? "Système";

            dict[u.Id] = (name, roleName);
        }

        return dict;
    }

    public async Task<ServiceResult<EmployeeReadDto>> GetCurrentAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Employee).FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user?.Employee == null) return ServiceResult<EmployeeReadDto>.Fail("Employé introuvable pour cet utilisateur.");
        return ServiceResult<EmployeeReadDto>.Ok(MapToRead(user.Employee));
    }

    public async Task<ServiceResult<DashboardResponseDto>> GetByCompanyAsync(int companyId, CancellationToken ct = default)
        => await GetAllAsync(companyId, ct);

    public async Task<ServiceResult<object>> GetSummaryAsync(int? companyId, CancellationToken ct = default)
    {
        var q = _db.Employees.AsQueryable();
        if (companyId.HasValue) q = q.Where(e => e.CompanyId == companyId.Value);
        var total = await q.CountAsync(ct);
        return ServiceResult<object>.Ok(new { totalEmployees = total });
    }

    public async Task<ServiceResult<IEnumerable<object>>> GetHistoryAsync(int employeeId, CancellationToken ct = default)
    {
        var contracts = await _db.EmployeeContracts.Where(c => c.EmployeeId == employeeId).OrderByDescending(c => c.StartDate).Select(c => new { c.Id, c.StartDate, c.EndDate, type = "contract" }).Take(50).ToListAsync(ct);
        return ServiceResult<IEnumerable<object>>.Ok(contracts.Cast<object>());
    }

    public async Task<ServiceResult<IEnumerable<EmployeeReadDto>>> GetByDepartementAsync(int departementId, CancellationToken ct = default)
    {
        var list = await _db.Employees.Where(e => e.DepartementId == departementId).Select(e => MapToRead(e)).ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<IEnumerable<EmployeeReadDto>>> GetSubordinatesAsync(int managerId, CancellationToken ct = default)
    {
        var list = await _db.Employees.Where(e => e.ManagerId == managerId).Select(e => MapToRead(e)).ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<EmployeeFormDataDto>> GetFormDataAsync(int? companyId, int requestingUserId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking()
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == requestingUserId && u.DeletedAt == null && u.IsActive, ct);

        if (user?.Employee is not { DeletedAt: null })
            return ServiceResult<EmployeeFormDataDto>.Fail("L'utilisateur n'est pas associé à un employé.");

        var userCompanyId = user.Employee!.CompanyId;

        int targetCompanyId;
        if (companyId.HasValue)
        {
            if (companyId.Value != userCompanyId)
            {
                var managerCompany = await _db.Companies.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == userCompanyId && c.DeletedAt == null, ct);
                var targetCompany = await _db.Companies.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == companyId.Value && c.DeletedAt == null, ct);
                if (targetCompany == null)
                    return ServiceResult<EmployeeFormDataDto>.Fail("Société cible non trouvée.");
                var allowedForExpert = managerCompany != null
                    && managerCompany.IsCabinetExpert
                    && targetCompany.ManagedByCompanyId == managerCompany.Id;
                if (!allowedForExpert)
                    return ServiceResult<EmployeeFormDataDto>.Fail("Accès refusé à cette société.");
            }
            targetCompanyId = companyId.Value;
        }
        else
            targetCompanyId = userCompanyId;

        var statuses = await _db.Statuses.AsNoTracking()
            .Where(s => s.IsActive && s.DeletedAt == null)
            .OrderBy(s => s.Code)
            .Select(s => new StatusFormDto { Id = s.Id, Name = s.NameFr })
            .ToListAsync(ct);

        var genders = await _db.Genders.AsNoTracking()
            .Where(g => g.IsActive && g.DeletedAt == null)
            .OrderBy(g => g.Code)
            .Select(g => new GenderFormDto { Id = g.Id, Name = g.NameFr })
            .ToListAsync(ct);

        var educationLevels = await _db.EducationLevels.AsNoTracking()
            .Where(e => e.IsActive && e.DeletedAt == null)
            .OrderBy(e => e.LevelOrder).ThenBy(e => e.NameFr)
            .Select(e => new EducationLevelFormDto { Id = e.Id, Name = e.NameFr })
            .ToListAsync(ct);

        var maritalStatuses = await _db.MaritalStatuses.AsNoTracking()
            .Where(m => m.DeletedAt == null)
            .OrderBy(m => m.NameFr)
            .Select(m => new MaritalStatusFormDto { Id = m.Id, Name = m.NameFr })
            .ToListAsync(ct);

        var nationalities = await _db.Nationalities.AsNoTracking()
            .Where(n => n.DeletedAt == null)
            .OrderBy(n => n.Name)
            .Select(n => new NationalityDto { Id = n.Id, Name = n.Name })
            .ToListAsync(ct);

        var countries = await _db.Countries.AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.CountryName)
            .Select(c => new CountryDto
            {
                Id = c.Id,
                CountryName = c.CountryName,
                CountryPhoneCode = c.CountryPhoneCode
            })
            .ToListAsync(ct);

        var cities = await _db.Cities.AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.CountryId).ThenBy(c => c.CityName)
            .Select(c => new CityDto
            {
                Id = c.Id,
                CityName = c.CityName,
                CountryId = c.CountryId,
                CountryName = c.Country != null ? c.Country.CountryName : null
            })
            .ToListAsync(ct);

        var departements = await _db.Departements.AsNoTracking()
            .Where(d => d.DeletedAt == null && d.CompanyId == targetCompanyId)
            .OrderBy(d => d.DepartementName)
            .Select(d => new DepartementDto { Id = d.Id, DepartementName = d.DepartementName, CompanyId = d.CompanyId })
            .ToListAsync(ct);

        var jobPositions = await _db.JobPositions.AsNoTracking()
            .Where(j => j.DeletedAt == null && j.CompanyId == targetCompanyId)
            .OrderBy(j => j.Name)
            .Select(j => new JobPositionDto { Id = j.Id, Name = j.Name, CompanyId = j.CompanyId })
            .ToListAsync(ct);

        var contractTypes = await _db.ContractTypes.AsNoTracking()
            .Where(c => c.DeletedAt == null && c.CompanyId == targetCompanyId)
            .OrderBy(c => c.ContractTypeName)
            .Select(c => new ContractTypeDto { Id = c.Id, ContractTypeName = c.ContractTypeName, CompanyId = c.CompanyId })
            .ToListAsync(ct);

        var potentialManagers = await _db.Employees.AsNoTracking()
            .Where(e => e.DeletedAt == null && e.CompanyId == targetCompanyId)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                FullName = e.FirstName + " " + e.LastName,
                DepartementName = e.Departement != null ? e.Departement.DepartementName : null
            })
            .ToListAsync(ct);

        var employeeCategories = await _db.EmployeeCategories.AsNoTracking()
            .Where(c => c.DeletedAt == null && c.CompanyId == targetCompanyId)
            .OrderBy(c => c.Name)
            .Select(c => new EmployeeCategorySimpleDto
            {
                Id = c.Id,
                Name = c.Name,
                Mode = c.Mode,
                PayrollPeriodicity = c.PayrollPeriodicity
            })
            .ToListAsync(ct);

        return ServiceResult<EmployeeFormDataDto>.Ok(new EmployeeFormDataDto
        {
            Statuses = statuses,
            Genders = genders,
            EducationLevels = educationLevels,
            MaritalStatuses = maritalStatuses,
            Nationalities = nationalities,
            Countries = countries,
            Cities = cities,
            Departements = departements,
            JobPositions = jobPositions,
            ContractTypes = contractTypes,
            PotentialManagers = potentialManagers,
            EmployeeCategories = employeeCategories
        });
    }

    // Crée un nouvel employé, aprés la création du employé, on doit crée un utilisateur lié à l'employé, envoyer un email d'invitation à l'employé pour qu'il active son compte et login.
    public async Task<ServiceResult<EmployeeReadDto>> CreateAsync(EmployeeCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var syncContract = dto.JobPositionId is > 0 && dto.ContractTypeId is > 0 && dto.StartDate.HasValue;
        var partialContract = (dto.JobPositionId is > 0 || dto.ContractTypeId is > 0 || dto.StartDate.HasValue) && !syncContract;
        if (partialContract)
            return ServiceResult<EmployeeReadDto>.Fail(
                "Pour enregistrer le contrat, le poste, le type de contrat et la date de début sont requis.");

        var wantMonthly = dto.Salary is > 0;
        var wantHourly = dto.SalaryHourly.HasValue && dto.SalaryHourly.Value > 0;
        if ((wantMonthly || wantHourly) && !syncContract)
            return ServiceResult<EmployeeReadDto>.Fail(
                "Pour enregistrer le salaire, renseignez le contrat (poste, type de contrat et date de début).");

        var companyId = dto.CompanyId ?? 0;
        if (syncContract && companyId < 1)
            return ServiceResult<EmployeeReadDto>.Fail("L'ID de la société est requis pour créer un contrat.");

        var e = new Domain.Entities.Employee.Employee
        {
            FirstName        = dto.FirstName,
            LastName         = dto.LastName,
            CinNumber        = dto.CinNumber,
            DateOfBirth      = dto.DateOfBirth,
            Email            = dto.Email,
            Phone            = dto.Phone,
            CompanyId        = companyId,
            DepartementId    = dto.DepartementId,
            ManagerId        = dto.ManagerId,
            StatusId         = dto.StatusId,
            GenderId         = dto.GenderId,
            NationalityId    = dto.NationalityId,
            EducationLevelId = dto.EducationLevelId,
            MaritalStatusId  = dto.MaritalStatusId,
            CnssNumber       = dto.CnssNumber,
            CimrNumber       = dto.CimrNumber,
            CategoryId       = dto.CategoryId,
            CreatedBy        = createdBy
        };

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.Employees.Add(e);
            await _db.SaveChangesAsync(ct);
            var id = e.Id;

            var hasAddressHints = !string.IsNullOrWhiteSpace(dto.AddressLine1)
                || !string.IsNullOrWhiteSpace(dto.AddressLine2)
                || !string.IsNullOrWhiteSpace(dto.ZipCode)
                || dto.CityId is > 0;
            if (hasAddressHints && dto.CityId is > 0)
            {
                var city = await _db.Cities.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.CityId!.Value && c.DeletedAt == null, ct);
                if (city == null)
                {
                    await transaction.RollbackAsync(ct);
                    return ServiceResult<EmployeeReadDto>.Fail("Ville introuvable.");
                }

                var line1 = dto.AddressLine1?.Trim() ?? string.Empty;
                var zip = dto.ZipCode?.Trim() ?? string.Empty;
                if (line1.Length >= 5 && zip.Length >= 4)
                {
                    var createAddr = await _addresses.CreateAsync(
                        new EmployeeAddressCreateDto
                        {
                            EmployeeId = id,
                            AddressLine1 = line1,
                            AddressLine2 = dto.AddressLine2,
                            ZipCode = zip,
                            CityId = dto.CityId!.Value,
                            CountryId = city.CountryId
                        },
                        createdBy,
                        ct);
                    if (!createAddr.Success)
                    {
                        await transaction.RollbackAsync(ct);
                        return ServiceResult<EmployeeReadDto>.Fail(createAddr.Error ?? "Création de l'adresse impossible.");
                    }
                }
            }

            int? newContractId = null;
            if (syncContract)
            {
                var createC = await _contracts.CreateAsync(
                    new EmployeeContractCreateDto
                    {
                        EmployeeId = id,
                        CompanyId = companyId,
                        JobPositionId = dto.JobPositionId!.Value,
                        ContractTypeId = dto.ContractTypeId!.Value,
                        StartDate = dto.StartDate!.Value,
                        EndDate = null
                    },
                    createdBy,
                    ct);
                if (!createC.Success || createC.Data == null)
                {
                    await transaction.RollbackAsync(ct);
                    return ServiceResult<EmployeeReadDto>.Fail(createC.Error ?? "Création du contrat impossible.");
                }

                newContractId = createC.Data.Id;
            }

            if (wantMonthly || wantHourly)
            {
                var effCreate = dto.SalaryEffectiveDate ?? dto.StartDate ?? DateTime.UtcNow.Date;
                var createSal = await _salaries.CreateAsync(
                    new EmployeeSalaryCreateDto
                    {
                        EmployeeId = id,
                        ContractId = newContractId!.Value,
                        BaseSalary = wantMonthly ? dto.Salary : null,
                        BaseSalaryHourly = wantHourly ? dto.SalaryHourly : null,
                        EffectiveDate = effCreate,
                        EndDate = null
                    },
                    createdBy,
                    ct);
                if (!createSal.Success)
                {
                    await transaction.RollbackAsync(ct);
                    return ServiceResult<EmployeeReadDto>.Fail(createSal.Error ?? "Création du salaire impossible.");
                }
            }

            await transaction.CommitAsync(ct);

            // Option 2: invitation automatique si le front indique un rôle
            _logger.LogInformation(
                "Employee created id={EmployeeId} InviteRoleId={InviteRoleId} CompanyId={CompanyId} Email={Email}",
                id,
                dto.InviteRoleId,
                companyId,
                dto.Email);
            if (dto.InviteRoleId.HasValue
                && dto.InviteRoleId.Value > 0
                && companyId > 0
                && !string.IsNullOrWhiteSpace(dto.Email))
            {
                // Envoi invitation employee (pas de mot de passe)
                await _invitationService.CreateEmployeeInvitationAsync(new InviteEmployeeDto
                {
                    Email = dto.Email,
                    CompanyId = companyId,
                    RoleId = dto.InviteRoleId.Value,
                    EmployeeId = id
                }, ct);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return ServiceResult<EmployeeReadDto>.Fail($"Erreur lors de la création : {ex.Message}");
        }

        var created = await _db.Employees
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Departement)
            .Include(x => x.Manager)
            .Include(x => x.Status)
            .Include(x => x.Category)
            .FirstAsync(x => x.Id == e.Id, ct);
        return ServiceResult<EmployeeReadDto>.Ok(MapToRead(created));
    }

    public async Task<ServiceResult<EmployeeReadDto>> UpdateAsync(int id, EmployeeUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var e = await _db.Employees
            .Include(emp => emp.Company)
            .Include(emp => emp.Status)
            .Include(emp => emp.Gender)
            .Include(emp => emp.MaritalStatus)
            .Include(emp => emp.Nationality)
            .Include(emp => emp.EducationLevel)
            .Include(emp => emp.Departement)
            .Include(emp => emp.Manager)
            .Include(emp => emp.Category)
            .FirstOrDefaultAsync(emp => emp.Id == id, ct);
        if (e == null) return ServiceResult<EmployeeReadDto>.Fail("Employé introuvable.");

        var pendingEmployeeLogs = new List<Func<Task>>();

        if (dto.FirstName     != null && dto.FirstName     != e.FirstName)
        {
            var prev = e.FirstName;
            var next = dto.FirstName;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.FirstNameChanged, prev, next, updatedBy, ct));
            e.FirstName = dto.FirstName;
        }
        if (dto.LastName      != null && dto.LastName      != e.LastName)
        {
            var prev = e.LastName;
            var next = dto.LastName;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.LastNameChanged, prev, next, updatedBy, ct));
            e.LastName = dto.LastName;
        }
        if (dto.CinNumber     != null && dto.CinNumber     != e.CinNumber)
        {
            var prev = e.CinNumber;
            var next = dto.CinNumber;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.CinNumberChanged, prev, next, updatedBy, ct));
            e.CinNumber = dto.CinNumber;
        }
        if (dto.DateOfBirth.HasValue && dto.DateOfBirth.Value != e.DateOfBirth)
        {
            var prev = e.DateOfBirth.ToString("yyyy-MM-dd");
            var next = dto.DateOfBirth.Value.ToString("yyyy-MM-dd");
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.DateOfBirthChanged, prev, next, updatedBy, ct));
            e.DateOfBirth = dto.DateOfBirth.Value;
        }
        if (dto.Email         != null && dto.Email         != e.Email)
        {
            var prev = e.Email;
            var next = dto.Email;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.EmailChanged, prev, next, updatedBy, ct));
            e.Email = dto.Email;
        }
        if (dto.Phone         != null && dto.Phone         != e.Phone)
        {
            var prev = e.Phone;
            var next = dto.Phone;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.PhoneChanged, prev, next, updatedBy, ct));
            e.Phone = dto.Phone;
        }
        if (dto.CnssNumber != null)
        {
            var nextCnss = string.IsNullOrWhiteSpace(dto.CnssNumber) ? null : dto.CnssNumber.Trim();
            if (!string.Equals(nextCnss, e.CnssNumber, StringComparison.Ordinal))
            {
                var prev = e.CnssNumber;
                pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.CnssNumberChanged, prev, nextCnss, updatedBy, ct));
                e.CnssNumber = nextCnss;
            }
        }
        if (dto.CimrNumber != null)
        {
            var nextCimr = string.IsNullOrWhiteSpace(dto.CimrNumber) ? null : dto.CimrNumber.Trim();
            if (!string.Equals(nextCimr, e.CimrNumber, StringComparison.Ordinal))
            {
                var prev = e.CimrNumber;
                pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.CimrNumberChanged, prev, nextCimr, updatedBy, ct));
                e.CimrNumber = nextCimr;
            }
        }
        if (dto.CimrEmployeeRate.HasValue && dto.CimrEmployeeRate.Value != e.CimrEmployeeRate)
            e.CimrEmployeeRate = dto.CimrEmployeeRate.Value;
        if (dto.CimrCompanyRate.HasValue && dto.CimrCompanyRate.Value != e.CimrCompanyRate)
            e.CimrCompanyRate = dto.CimrCompanyRate.Value;
        if (dto.HasPrivateInsurance.HasValue && dto.HasPrivateInsurance.Value != e.HasPrivateInsurance)
            e.HasPrivateInsurance = dto.HasPrivateInsurance.Value;
        if (dto.PrivateInsuranceNumber != null && dto.PrivateInsuranceNumber != e.PrivateInsuranceNumber)
            e.PrivateInsuranceNumber = string.IsNullOrWhiteSpace(dto.PrivateInsuranceNumber) ? null : dto.PrivateInsuranceNumber.Trim();
        if (dto.PrivateInsuranceRate.HasValue && dto.PrivateInsuranceRate.Value != e.PrivateInsuranceRate)
            e.PrivateInsuranceRate = dto.PrivateInsuranceRate.Value;
        if (dto.DisableAmo.HasValue && dto.DisableAmo.Value != e.DisableAmo)
            e.DisableAmo = dto.DisableAmo.Value;
        if (dto.PaymentMethod != null && dto.PaymentMethod != e.PaymentMethod)
            e.PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? null : dto.PaymentMethod.Trim();

        if (dto.DepartementId != null && dto.DepartementId != e.DepartementId)
        {
            var oldName = e.Departement?.DepartementName;
            var newDep  = dto.DepartementId.HasValue
                ? await _db.Departements.AsNoTracking().Where(d => d.Id == dto.DepartementId.Value).Select(d => d.DepartementName).FirstOrDefaultAsync(ct)
                : null;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.DepartmentChanged, oldName, newDep, updatedBy, ct));
            e.DepartementId = dto.DepartementId;
        }
        if (dto.ManagerId     != null && dto.ManagerId     != e.ManagerId)
        {
            var oldMgr = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}".Trim() : null;
            var newMgr = dto.ManagerId.HasValue
                ? await _db.Employees.AsNoTracking().Where(m => m.Id == dto.ManagerId.Value).Select(m => m.FirstName + " " + m.LastName).FirstOrDefaultAsync(ct)
                : null;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.ManagerChanged, oldMgr, newMgr, updatedBy, ct));
            e.ManagerId = dto.ManagerId;
        }
        if (dto.StatusId      != null && dto.StatusId      != e.StatusId)
        {
            var oldStatus = e.Status?.Code;
            var newStatus = dto.StatusId.HasValue
                ? await _db.Statuses.AsNoTracking().Where(s => s.Id == dto.StatusId.Value).Select(s => s.Code).FirstOrDefaultAsync(ct)
                : null;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.StatusChanged, oldStatus, newStatus, updatedBy, ct));
            e.StatusId = dto.StatusId;
        }
        if (dto.GenderId      != null && dto.GenderId      != e.GenderId)
        {
            var oldGender = e.Gender?.Code;
            var newGender = dto.GenderId.HasValue
                ? await _db.Genders.AsNoTracking().Where(g => g.Id == dto.GenderId.Value).Select(g => g.Code).FirstOrDefaultAsync(ct)
                : null;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.GenderChanged, oldGender, newGender, updatedBy, ct));
            e.GenderId = dto.GenderId;
        }
        if (dto.NationalityId != null && dto.NationalityId != e.NationalityId)
        {
            var oldNat = e.Nationality?.Name;
            var newNat = dto.NationalityId.HasValue
                ? await _db.Nationalities.AsNoTracking().Where(n => n.Id == dto.NationalityId.Value).Select(n => n.Name).FirstOrDefaultAsync(ct)
                : null;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.NationalityChanged, oldNat, newNat, updatedBy, ct));
            e.NationalityId = dto.NationalityId;
        }
        if (dto.EducationLevelId != null && dto.EducationLevelId != e.EducationLevelId)
        {
            var oldEdu = e.EducationLevel?.Code;
            var newEdu = dto.EducationLevelId.HasValue
                ? await _db.EducationLevels.AsNoTracking().Where(el => el.Id == dto.EducationLevelId.Value).Select(el => el.Code).FirstOrDefaultAsync(ct)
                : null;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.EducationLevelChanged, oldEdu, newEdu, updatedBy, ct));
            e.EducationLevelId = dto.EducationLevelId;
        }
        if (dto.MaritalStatusId != null && dto.MaritalStatusId != e.MaritalStatusId)
        {
            var oldMs = e.MaritalStatus?.Code;
            var newMs = dto.MaritalStatusId.HasValue
                ? await _db.MaritalStatuses.AsNoTracking().Where(ms => ms.Id == dto.MaritalStatusId.Value).Select(ms => ms.Code).FirstOrDefaultAsync(ct)
                : null;
            pendingEmployeeLogs.Add(() => _eventLog.LogSimpleEventAsync(id, EmployeeEventLogNames.MaritalStatusChanged, oldMs, newMs, updatedBy, ct));
            e.MaritalStatusId = dto.MaritalStatusId;
        }
        if (dto.CategoryId    != null) e.CategoryId    = dto.CategoryId;

        e.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);

        foreach (var runLog in pendingEmployeeLogs)
            await runLog();

        var addressPatch =
            dto.AddressLine1 != null || dto.AddressLine2 != null || dto.ZipCode != null || dto.CityId != null;
        if (addressPatch)
        {
            var existingAddr = await _db.EmployeeAddresses
                .Where(a => a.EmployeeId == id && a.DeletedAt == null)
                .OrderBy(a => a.Id)
                .FirstOrDefaultAsync(ct);

            if (existingAddr != null)
            {
                var addrDto = new EmployeeAddressUpdateDto
                {
                    AddressLine1 = dto.AddressLine1,
                    AddressLine2 = dto.AddressLine2,
                    ZipCode = dto.ZipCode,
                    CityId = dto.CityId
                };
                var addrRes = await _addresses.UpdateAsync(existingAddr.Id, addrDto, updatedBy, ct);
                if (!addrRes.Success)
                    return ServiceResult<EmployeeReadDto>.Fail(addrRes.Error ?? "Mise à jour de l'adresse impossible.");
            }
            else
            {
                var cityId = dto.CityId;
                if (cityId is null or < 1)
                    return ServiceResult<EmployeeReadDto>.Fail("Sélectionnez une ville pour enregistrer l'adresse.");

                var city = await _db.Cities.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == cityId.Value && c.DeletedAt == null, ct);
                if (city == null)
                    return ServiceResult<EmployeeReadDto>.Fail("Ville introuvable.");

                var line1 = dto.AddressLine1?.Trim() ?? string.Empty;
                var zip = dto.ZipCode?.Trim() ?? string.Empty;
                if (line1.Length < 5 || zip.Length < 4)
                    return ServiceResult<EmployeeReadDto>.Fail(
                        "Adresse incomplète : ligne 1 (min. 5 caractères), code postal (min. 4 caractères) et ville sont requis.");

                var createRes = await _addresses.CreateAsync(
                    new EmployeeAddressCreateDto
                    {
                        EmployeeId = id,
                        AddressLine1 = line1,
                        AddressLine2 = dto.AddressLine2,
                        ZipCode = zip,
                        CityId = cityId.Value,
                        CountryId = city.CountryId
                    },
                    updatedBy,
                    ct);
                if (!createRes.Success)
                    return ServiceResult<EmployeeReadDto>.Fail(createRes.Error ?? "Création de l'adresse impossible.");
            }
        }

        var syncContract = dto.JobPositionId is > 0
            || dto.ContractTypeId is > 0
            || dto.ContractStartDate.HasValue;
        if (syncContract)
        {
            var contractList = await _db.EmployeeContracts
                .Where(c => c.EmployeeId == id && c.DeletedAt == null)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync(ct);

            var activeContract = contractList.FirstOrDefault(c => c.EndDate == null) ?? contractList.FirstOrDefault();

            if (activeContract != null)
            {
                var contractPatch = new EmployeeContractUpdateDto();
                if (dto.JobPositionId is > 0) contractPatch.JobPositionId = dto.JobPositionId;
                if (dto.ContractTypeId is > 0) contractPatch.ContractTypeId = dto.ContractTypeId;
                if (dto.ContractStartDate.HasValue) contractPatch.StartDate = dto.ContractStartDate;

                if (contractPatch.JobPositionId != null || contractPatch.ContractTypeId != null || contractPatch.StartDate != null)
                {
                    var cRes = await _contracts.UpdateAsync(activeContract.Id, contractPatch, updatedBy, ct);
                    if (!cRes.Success)
                        return ServiceResult<EmployeeReadDto>.Fail(cRes.Error ?? "Mise à jour du contrat impossible.");
                }
            }
            else if (dto.JobPositionId is > 0 && dto.ContractTypeId is > 0 && e.CompanyId > 0)
            {
                var start = dto.ContractStartDate ?? DateTime.UtcNow.Date;
                var createC = await _contracts.CreateAsync(
                    new EmployeeContractCreateDto
                    {
                        EmployeeId = id,
                        CompanyId = e.CompanyId,
                        JobPositionId = dto.JobPositionId.Value,
                        ContractTypeId = dto.ContractTypeId.Value,
                        StartDate = start,
                        EndDate = null
                    },
                    updatedBy,
                    ct);
                if (!createC.Success)
                    return ServiceResult<EmployeeReadDto>.Fail(createC.Error ?? "Création du contrat impossible.");
            }
        }

        var wantMonthly = dto.Salary is > 0;
        var wantHourly = dto.SalaryHourly.HasValue;
        var wantSalaryDate = dto.SalaryEffectiveDate.HasValue;
        if (wantMonthly || wantHourly || wantSalaryDate)
        {
            var salRows = await _db.EmployeeSalaries
                .Where(s => s.EmployeeId == id && s.DeletedAt == null)
                .OrderByDescending(s => s.EffectiveDate)
                .ToListAsync(ct);
            var activeSal = salRows.FirstOrDefault(s => s.EndDate == null) ?? salRows.FirstOrDefault();
            if (activeSal != null)
            {
                var salPatch = new EmployeeSalaryUpdateDto();
                if (wantMonthly) salPatch.BaseSalary = dto.Salary!.Value;
                if (wantHourly) salPatch.BaseSalaryHourly = dto.SalaryHourly;
                if (wantSalaryDate) salPatch.EffectiveDate = dto.SalaryEffectiveDate;
                if (salPatch.BaseSalary != null || salPatch.BaseSalaryHourly != null || salPatch.EffectiveDate != null)
                {
                    var salRes = await _salaries.UpdateAsync(activeSal.Id, salPatch, updatedBy, ct);
                    if (!salRes.Success)
                        return ServiceResult<EmployeeReadDto>.Fail(salRes.Error ?? "Mise à jour du salaire impossible.");
                }
            }
            else if (wantMonthly)
            {
                var contractList = await _db.EmployeeContracts
                    .Where(c => c.EmployeeId == id && c.DeletedAt == null)
                    .OrderByDescending(c => c.StartDate)
                    .ToListAsync(ct);
                var activeContract = contractList.FirstOrDefault(c => c.EndDate == null) ?? contractList.FirstOrDefault();
                if (activeContract == null)
                    return ServiceResult<EmployeeReadDto>.Fail(
                        "Aucun contrat actif : enregistrez un contrat avant d'associer un salaire.");

                var effCreate = dto.SalaryEffectiveDate ?? DateTime.UtcNow.Date;
                var createSal = await _salaries.CreateAsync(
                    new EmployeeSalaryCreateDto
                    {
                        EmployeeId = id,
                        ContractId = activeContract.Id,
                        BaseSalary = dto.Salary!.Value,
                        BaseSalaryHourly = dto.SalaryHourly,
                        EffectiveDate = effCreate,
                        EndDate = null
                    },
                    updatedBy,
                    ct);
                if (!createSal.Success)
                    return ServiceResult<EmployeeReadDto>.Fail(createSal.Error ?? "Création du salaire impossible.");
            }
        }

        return ServiceResult<EmployeeReadDto>.Ok(MapToRead(e));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var e = await _db.Employees.FindAsync(new object[] { id }, ct);
        if (e == null) return ServiceResult.Fail("Employé introuvable.");
        e.DeletedAt = DateTimeOffset.UtcNow; e.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct); return ServiceResult.Ok();
    }

    public async Task<ServiceResult<SageImportResultDto>> ImportFromSageAsync( Stream csvStream, int? companyId, int userId, int? month, int? year, bool preview, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking()
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive, ct);

        if (user?.Employee is not { DeletedAt: null })
            return ServiceResult<SageImportResultDto>.Fail("L'utilisateur n'est pas associé à un employé.");

        var userCompanyId = user.Employee!.CompanyId;

        int targetCompanyId;
        if (companyId.HasValue)
        {
            if (companyId.Value != userCompanyId)
            {
                var managerCompany = await _db.Companies.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == userCompanyId && c.DeletedAt == null, ct);
                var targetCompany = await _db.Companies.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == companyId.Value && c.DeletedAt == null, ct);
                if (targetCompany == null)
                    return ServiceResult<SageImportResultDto>.Fail("Société cible non trouvée.");
                var allowedForExpert = managerCompany != null
                    && managerCompany.IsCabinetExpert
                    && targetCompany.ManagedByCompanyId == managerCompany.Id;
                if (!allowedForExpert)
                    return ServiceResult<SageImportResultDto>.Fail("Accès refusé à cette société.");
            }

            targetCompanyId = companyId.Value;
        }
        else
            targetCompanyId = userCompanyId;

        return await EmployeeSagePayImport.ExecuteAsync(
            _db, _eventLog, csvStream, targetCompanyId, userId, month, year, preview, ct);
    }

    // ── Contract ─────────────────────────────────────────────────────────────

    public Task<ServiceResult<IEnumerable<EmployeeContractReadDto>>> GetContractsAsync(int employeeId, CancellationToken ct = default)
        => _contracts.GetByEmployeeAsync(employeeId, ct);

    public Task<ServiceResult<EmployeeContractReadDto>> CreateContractAsync(EmployeeContractCreateDto dto, int createdBy, CancellationToken ct = default)
        => _contracts.CreateAsync(dto, createdBy, ct);

    public Task<ServiceResult<EmployeeContractReadDto>> UpdateContractAsync(int id, EmployeeContractUpdateDto dto, int updatedBy, CancellationToken ct = default)
        => _contracts.UpdateAsync(id, dto, updatedBy, ct);

    public Task<ServiceResult> DeleteContractAsync(int id, int deletedBy, CancellationToken ct = default)
        => _contracts.DeleteAsync(id, deletedBy, ct);

    // ── Salary ───────────────────────────────────────────────────────────────

    public Task<ServiceResult<IEnumerable<EmployeeSalaryReadDto>>> GetSalariesAsync(int employeeId, CancellationToken ct = default)
        => _salaries.GetByEmployeeAsync(employeeId, ct);

    public Task<ServiceResult<EmployeeSalaryReadDto>> CreateSalaryAsync(EmployeeSalaryCreateDto dto, int createdBy, CancellationToken ct = default)
        => _salaries.CreateAsync(dto, createdBy, ct);

    public Task<ServiceResult<EmployeeSalaryReadDto>> UpdateSalaryAsync(int id, EmployeeSalaryUpdateDto dto, int updatedBy, CancellationToken ct = default)
        => _salaries.UpdateAsync(id, dto, updatedBy, ct);

    public Task<ServiceResult> DeleteSalaryAsync(int id, int deletedBy, CancellationToken ct = default)
        => _salaries.DeleteAsync(id, deletedBy, ct);

    // ── SalaryComponent ──────────────────────────────────────────────────────

    public Task<ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>> GetSalaryComponentsAsync(int salaryId, CancellationToken ct = default)
        => _salaries.GetComponentsAsync(salaryId, ct);

    public Task<ServiceResult<EmployeeSalaryComponentReadDto>> CreateSalaryComponentAsync(EmployeeSalaryComponentCreateDto dto, int createdBy, CancellationToken ct = default)
        => _salaries.CreateComponentAsync(dto, createdBy, ct);

    public Task<ServiceResult<EmployeeSalaryComponentReadDto>> UpdateSalaryComponentAsync(int id, EmployeeSalaryComponentUpdateDto dto, int updatedBy, CancellationToken ct = default)
        => _salaries.UpdateComponentAsync(id, dto, updatedBy, ct);

    public Task<ServiceResult> DeleteSalaryComponentAsync(int id, int deletedBy, CancellationToken ct = default)
        => _salaries.DeleteComponentAsync(id, deletedBy, ct);

    // ── Address ──────────────────────────────────────────────────────────────

    public Task<ServiceResult<IEnumerable<EmployeeAddressReadDto>>> GetAddressesAsync(int employeeId, CancellationToken ct = default)
        => _addresses.GetByEmployeeAsync(employeeId, ct);

    public Task<ServiceResult<EmployeeAddressReadDto>> CreateAddressAsync(EmployeeAddressCreateDto dto, int createdBy, CancellationToken ct = default)
        => _addresses.CreateAsync(dto, createdBy, ct);

    public Task<ServiceResult<EmployeeAddressReadDto>> UpdateAddressAsync(int id, EmployeeAddressUpdateDto dto, int updatedBy, CancellationToken ct = default)
        => _addresses.UpdateAsync(id, dto, updatedBy, ct);

    public Task<ServiceResult> DeleteAddressAsync(int id, int deletedBy, CancellationToken ct = default)
        => _addresses.DeleteAsync(id, deletedBy, ct);

    // ── Document ─────────────────────────────────────────────────────────────

    public Task<ServiceResult<IEnumerable<EmployeeDocumentReadDto>>> GetDocumentsAsync(int employeeId, CancellationToken ct = default)
        => _documents.GetByEmployeeAsync(employeeId, ct);

    public Task<ServiceResult<EmployeeDocumentReadDto>> CreateDocumentAsync(EmployeeDocumentCreateDto dto, int createdBy, CancellationToken ct = default)
        => _documents.CreateAsync(dto, createdBy, ct);

    public Task<ServiceResult<EmployeeDocumentReadDto>> UpdateDocumentAsync(int id, EmployeeDocumentUpdateDto dto, int updatedBy, CancellationToken ct = default)
        => _documents.UpdateAsync(id, dto, updatedBy, ct);

    public Task<ServiceResult> DeleteDocumentAsync(int id, int deletedBy, CancellationToken ct = default)
        => _documents.DeleteAsync(id, deletedBy, ct);

    // ── Child ────────────────────────────────────────────────────────────────

    public Task<ServiceResult<IEnumerable<EmployeeChildReadDto>>> GetChildrenAsync(int employeeId, CancellationToken ct = default)
        => _family.GetChildrenAsync(employeeId, ct);

    public Task<ServiceResult<EmployeeChildReadDto>> CreateChildAsync(EmployeeChildCreateDto dto, int createdBy, CancellationToken ct = default)
        => _family.CreateChildAsync(dto, createdBy, ct);

    public Task<ServiceResult<EmployeeChildReadDto>> UpdateChildAsync(int id, EmployeeChildUpdateDto dto, int updatedBy, CancellationToken ct = default)
        => _family.UpdateChildAsync(id, dto, updatedBy, ct);

    public Task<ServiceResult> DeleteChildAsync(int id, int deletedBy, CancellationToken ct = default)
        => _family.DeleteChildAsync(id, deletedBy, ct);

    // ── Spouse ───────────────────────────────────────────────────────────────

    public Task<ServiceResult<IEnumerable<EmployeeSpouseReadDto>>> GetSpousesAsync(int employeeId, CancellationToken ct = default)
        => _family.GetSpousesAsync(employeeId, ct);

    public Task<ServiceResult<EmployeeSpouseReadDto>> CreateSpouseAsync(EmployeeSpouseCreateDto dto, int createdBy, CancellationToken ct = default)
        => _family.CreateSpouseAsync(dto, createdBy, ct);

    public Task<ServiceResult<EmployeeSpouseReadDto>> UpdateSpouseAsync(int id, EmployeeSpouseUpdateDto dto, int updatedBy, CancellationToken ct = default)
        => _family.UpdateSpouseAsync(id, dto, updatedBy, ct);

    public Task<ServiceResult> DeleteSpouseAsync(int id, int deletedBy, CancellationToken ct = default)
        => _family.DeleteSpouseAsync(id, deletedBy, ct);

    public Task<ServiceResult<EmployeeSpouseReadDto>> UpdateSpouseByEmployeeAsync(int employeeId, EmployeeSpouseUpdateDto dto, int updatedBy, CancellationToken ct = default)
        => _family.UpdateSpouseByEmployeeAsync(employeeId, dto, updatedBy, ct);

    public Task<ServiceResult> DeleteSpouseByEmployeeAsync(int employeeId, int deletedBy, CancellationToken ct = default)
        => _family.DeleteSpouseByEmployeeAsync(employeeId, deletedBy, ct);

    // ── Absence ──────────────────────────────────────────────────────────────

    public Task<ServiceResult<IEnumerable<EmployeeAbsenceReadDto>>> GetAbsencesAsync(int employeeId, CancellationToken ct = default)
        => _absences.GetByEmployeeAsync(employeeId, ct);

    public Task<ServiceResult<EmployeeAbsenceStatsDto>> GetAbsenceStatsAsync(int companyId, int? employeeId, CancellationToken ct = default)
        => _absences.GetStatsAsync(companyId, employeeId, ct);

    public Task<ServiceResult<EmployeeAbsenceReadDto>> CreateAbsenceAsync(EmployeeAbsenceCreateDto dto, int createdBy, CancellationToken ct = default)
        => _absences.CreateAsync(dto, createdBy, ct);

    public Task<ServiceResult<EmployeeAbsenceReadDto>> UpdateAbsenceAsync(int id, EmployeeAbsenceUpdateDto dto, int updatedBy, CancellationToken ct = default)
        => _absences.UpdateAsync(id, dto, updatedBy, ct);

    public Task<ServiceResult<EmployeeAbsenceReadDto>> DecideAbsenceAsync(int id, EmployeeAbsenceDecisionDto dto, int decidedBy, CancellationToken ct = default)
        => _absences.DecideAsync(id, dto, decidedBy, ct);

    public async Task<ServiceResult> CancelAbsenceAsync(int id, EmployeeAbsenceCancellationDto dto, int cancelledBy, CancellationToken ct = default)
    {
        var r = await _absences.CancelAsync(id, dto, cancelledBy, ct);
        return r.Success ? ServiceResult.Ok() : ServiceResult.Fail(r.Error!);
    }

    public Task<ServiceResult> DeleteAbsenceAsync(int id, int deletedBy, CancellationToken ct = default)
        => _absences.DeleteAsync(id, deletedBy, ct);

    // ── Overtime ─────────────────────────────────────────────────────────────

    public Task<ServiceResult<IEnumerable<EmployeeOvertimeListDto>>> GetOvertimesAsync(int employeeId, CancellationToken ct = default)
        => _overtimes.GetByEmployeeAsync(employeeId, ct);

    public Task<ServiceResult<EmployeeOvertimeReadDto>> GetOvertimeByIdAsync(int id, CancellationToken ct = default)
        => _overtimes.GetByIdAsync(id, ct);

    public Task<ServiceResult<EmployeeOvertimeCreateOutcomeDto>> CreateOvertimeAsync(EmployeeOvertimeCreateDto dto, int createdBy, CancellationToken ct = default)
        => _overtimes.CreateAsync(dto, createdBy, ct);

    public Task<ServiceResult<EmployeeOvertimeReadDto>> UpdateOvertimeAsync(int id, EmployeeOvertimeUpdateDto dto, int updatedBy, CancellationToken ct = default)
        => _overtimes.UpdateAsync(id, dto, updatedBy, ct);

    public Task<ServiceResult<EmployeeOvertimeReadDto>> SubmitOvertimeAsync(int id, EmployeeOvertimeSubmitDto dto, int userId, CancellationToken ct = default)
        => _overtimes.SubmitAsync(id, dto, userId, ct);

    public Task<ServiceResult<EmployeeOvertimeReadDto>> DecideOvertimeAsync(int id, EmployeeOvertimeApprovalDto dto, int decidedBy, CancellationToken ct = default)
        => _overtimes.DecideAsync(id, dto, decidedBy, ct);

    public Task<ServiceResult> DeleteOvertimeAsync(int id, int deletedBy, CancellationToken ct = default)
        => _overtimes.DeleteAsync(id, deletedBy, ct);

    // ── Attendance ───────────────────────────────────────────────────────────

    public Task<ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>> GetAttendancesAsync(int employeeId, DateOnly? from, DateOnly? to, CancellationToken ct = default)
        => _attendances.GetByEmployeeAsync(employeeId, from, to, false, ct);

    public Task<ServiceResult<EmployeeAttendanceReadDto>> CreateAttendanceAsync(EmployeeAttendanceCreateDto dto, int createdBy, CancellationToken ct = default)
        => _attendances.CreateAsync(dto, createdBy, ct);

    public Task<ServiceResult<TimesheetImportResultDto>> ImportTimesheetAsync(int companyId, int month, int year, IEnumerable<object> rows, int userId, CancellationToken ct = default)
        => _attendances.ImportTimesheetAsync(companyId, month, year, rows, userId, ct);

    // ── Category ─────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<EmployeeCategoryReadDto>>> GetCategoriesAsync(int companyId, CancellationToken ct = default)
    {
        var list = await _db.EmployeeCategories.Where(c => c.CompanyId == companyId).Select(c => new EmployeeCategoryReadDto { Id = c.Id, Name = c.Name, CompanyId = c.CompanyId }).ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeCategoryReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<IEnumerable<EmployeeCategoryReadDto>>> GetCategoriesByModeAsync(EmployeeCategoryMode mode, int? companyId, CancellationToken ct = default)
    {
        var q = _db.EmployeeCategories.AsNoTracking().Where(c => c.DeletedAt == null && c.Mode == mode);
        if (companyId.HasValue)
            q = q.Where(c => c.CompanyId == companyId.Value);
        var list = await q
            .Include(c => c.Company)
            .OrderBy(c => c.Company!.CompanyName)
            .ThenBy(c => c.Name)
            .Select(c => new EmployeeCategoryReadDto
            {
                Id = c.Id,
                CompanyId = c.CompanyId,
                CompanyName = c.Company!.CompanyName,
                Name = c.Name,
                Mode = c.Mode,
                PayrollPeriodicity = c.PayrollPeriodicity,
                ModeDescription = c.Mode == EmployeeCategoryMode.Attendance ? "Présence" : "Absence",
                CreatedAt = c.CreatedAt.DateTime
            })
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeCategoryReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<EmployeeCategoryReadDto>> GetCategoryByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await _db.EmployeeCategories.FindAsync(new object[] { id }, ct);
        return c == null ? ServiceResult<EmployeeCategoryReadDto>.Fail("Catégorie introuvable.") : ServiceResult<EmployeeCategoryReadDto>.Ok(new EmployeeCategoryReadDto { Id = c.Id, Name = c.Name, CompanyId = c.CompanyId });
    }

    public async Task<ServiceResult<EmployeeCategoryReadDto>> CreateCategoryAsync(EmployeeCategoryCreateDto dto, int createdBy, CancellationToken ct = default)
    {
        var c = new EmployeeCategory { Name = dto.Name, CompanyId = dto.CompanyId, CreatedBy = createdBy };
        _db.EmployeeCategories.Add(c); await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeCategoryReadDto>.Ok(new EmployeeCategoryReadDto { Id = c.Id, Name = c.Name, CompanyId = c.CompanyId });
    }

    public async Task<ServiceResult<EmployeeCategoryReadDto>> UpdateCategoryAsync(int id, EmployeeCategoryUpdateDto dto, int updatedBy, CancellationToken ct = default)
    {
        var c = await _db.EmployeeCategories.FindAsync(new object[] { id }, ct);
        if (c == null) return ServiceResult<EmployeeCategoryReadDto>.Fail("Catégorie introuvable.");
        if (dto.Name != null) c.Name = dto.Name;
        c.UpdatedBy = updatedBy; await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeCategoryReadDto>.Ok(new EmployeeCategoryReadDto { Id = c.Id, Name = c.Name, CompanyId = c.CompanyId });
    }

    public async Task<ServiceResult> DeleteCategoryAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var c = await _db.EmployeeCategories.FindAsync(new object[] { id }, ct);
        if (c == null) return ServiceResult.Fail("Catégorie introuvable.");
        c.DeletedAt = DateTimeOffset.UtcNow; c.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct); return ServiceResult.Ok();
    }

    // ── Mapper ───────────────────────────────────────────────────────────────

    private static EmployeeReadDto MapToRead(Domain.Entities.Employee.Employee e) => new()
    {
        Id              = e.Id,
        Matricule       = e.Matricule,
        FirstName       = e.FirstName,
        LastName        = e.LastName,
        CinNumber       = e.CinNumber,
        DateOfBirth     = e.DateOfBirth,
        Email           = e.Email,
        Phone           = e.Phone,
        CompanyId       = e.CompanyId,
        CompanyName     = e.Company?.CompanyName ?? string.Empty,
        DepartementId   = e.DepartementId,
        DepartementName = e.Departement?.DepartementName,
        ManagerId       = e.ManagerId,
        ManagerName     = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}".Trim() : null,
        StatusId        = e.StatusId,
        StatusName      = e.Status?.Code ?? string.Empty,
        GenderId        = e.GenderId,
        NationalityId   = e.NationalityId,
        EducationLevelId = e.EducationLevelId,
        MaritalStatusId = e.MaritalStatusId,
        CategoryId      = e.CategoryId,
        CategoryName    = e.Category?.Name,
        CnssNumber      = e.CnssNumber,
        CimrNumber      = e.CimrNumber,
        CimrEmployeeRate = e.CimrEmployeeRate?.ToString("F2", CultureInfo.InvariantCulture),
        CimrCompanyRate  = e.CimrCompanyRate?.ToString("F2", CultureInfo.InvariantCulture),
        HasPrivateInsurance = e.HasPrivateInsurance,
        DisableAmo      = e.DisableAmo,
        PrivateInsuranceNumber = e.PrivateInsuranceNumber,
        PrivateInsuranceRate   = e.PrivateInsuranceRate,
        CreatedAt       = e.CreatedAt.DateTime
    };
}
