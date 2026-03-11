using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Dashboard.Dtos;
using System.Globalization;

namespace payzen_backend.Services.Dashboard
{
    public class DashboardHrService : IDashboardHrService
    {
        private static readonly string[] ActiveStatusTokens = ["active", "actif", "enabled"];
        private static readonly string[] InactiveStatusTokens = ["inactive", "inactif", "resigned", "terminated", "departed", "left", "fired", "archive"];

        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardHrService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<DashboardHrRawDto> GetHrDashboardRawAsync(int? companyId, string? month, CancellationToken cancellationToken = default)
        {
            var context = await BuildContextAsync(companyId, month, cancellationToken);

            if (context.Employees.Count > 10000 || context.Contracts.Count > 50000 || context.Salaries.Count > 100000)
            {
                Console.WriteLine(
                    $"[DashboardHrService] Large raw payload detected. employees={context.Employees.Count}, contracts={context.Contracts.Count}, salaries={context.Salaries.Count}, companyId={context.CompanyId}, month={context.MonthStart:yyyy-MM}");
            }

            return new DashboardHrRawDto
            {
                Meta = new DashboardHrMetaDto
                {
                    CompanyId = context.CompanyId,
                    CompanyName = context.CompanyName,
                    Month = context.MonthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    GeneratedAt = DateTimeOffset.UtcNow
                },
                Employees = context.Employees.Select(e => new DashboardHrRawEmployeeDto
                {
                    Id = e.Id,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Department = e.Department,
                    StatusCode = e.StatusCode,
                    GenderCode = e.GenderCode
                }).ToList(),
                Contracts = context.Contracts.Select(c => new DashboardHrRawContractDto
                {
                    EmployeeId = c.EmployeeId,
                    StartDate = DateOnly.FromDateTime(c.StartDate),
                    EndDate = c.EndDate.HasValue ? DateOnly.FromDateTime(c.EndDate.Value) : null,
                    Position = c.Position,
                    ContractType = c.ContractType
                }).ToList(),
                Salaries = context.Salaries.Select(s => new DashboardHrRawSalaryDto
                {
                    EmployeeId = s.EmployeeId,
                    BaseSalary = s.BaseSalary,
                    EffectiveDate = DateOnly.FromDateTime(s.EffectiveDate),
                    EndDate = s.EndDate.HasValue ? DateOnly.FromDateTime(s.EndDate.Value) : null
                }).ToList()
            };
        }

        public async Task<DashboardHrDto> GetHrDashboardAsync(int? companyId, string? month, CancellationToken cancellationToken = default)
        {
            var context = await BuildContextAsync(companyId, month, cancellationToken);

            return new DashboardHrDto
            {
                Meta = new DashboardHrMetaDto
                {
                    CompanyId = context.CompanyId,
                    CompanyName = context.CompanyName,
                    Month = context.MonthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    GeneratedAt = DateTimeOffset.UtcNow
                },
                VueGlobale = BuildVueGlobale(context),
                MouvementsRh = BuildMouvements(context),
                MasseSalariale = BuildMasseSalariale(context),
                PariteDiversite = BuildPariteDiversite(context),
                ConformiteSociale = BuildConformiteSociale(context)
            };
        }

        public async Task<DashboardHrVueGlobaleDto> GetVueGlobaleAsync(int? companyId, string? month, CancellationToken cancellationToken = default)
            => BuildVueGlobale(await BuildContextAsync(companyId, month, cancellationToken));

        public async Task<DashboardHrMouvementsDto> GetMouvementsRhAsync(int? companyId, string? month, CancellationToken cancellationToken = default)
            => BuildMouvements(await BuildContextAsync(companyId, month, cancellationToken));

        public async Task<DashboardHrMasseSalarialeDto> GetMasseSalarialeAsync(int? companyId, string? month, CancellationToken cancellationToken = default)
            => BuildMasseSalariale(await BuildContextAsync(companyId, month, cancellationToken));

        public async Task<DashboardHrPariteDiversiteDto> GetPariteDiversiteAsync(int? companyId, string? month, CancellationToken cancellationToken = default)
            => BuildPariteDiversite(await BuildContextAsync(companyId, month, cancellationToken));

        public async Task<DashboardHrConformiteSocialeDto> GetConformiteSocialeAsync(int? companyId, string? month, CancellationToken cancellationToken = default)
            => BuildConformiteSociale(await BuildContextAsync(companyId, month, cancellationToken));

        private async Task<DashboardBuildContext> BuildContextAsync(int? requestedCompanyId, string? month, CancellationToken cancellationToken)
        {
            var monthStart = ParseMonth(month);
            var monthEnd = EndOfMonth(monthStart);

            var access = await ResolveCompanyAccessAsync(requestedCompanyId, cancellationToken);

            var employees = await _db.Employees
                .AsNoTracking()
                .Where(e => e.CompanyId == access.CompanyId)
                .Select(e => new EmployeeSnapshot
                {
                    Id = e.Id,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Department = e.Departement != null ? e.Departement.DepartementName : "Autres",
                    StatusCode = e.Status != null ? e.Status.Code : string.Empty,
                    GenderCode = e.Gender != null ? e.Gender.Code : string.Empty
                })
                .ToListAsync(cancellationToken);

            var contracts = await _db.EmployeeContracts
                .AsNoTracking()
                .Where(c => c.CompanyId == access.CompanyId)
                .Select(c => new ContractSnapshot
                {
                    EmployeeId = c.EmployeeId,
                    StartDate = c.StartDate.Date,
                    EndDate = c.EndDate.HasValue ? c.EndDate.Value.Date : null,
                    Position = c.JobPosition != null ? c.JobPosition.Name : "Non assigne",
                    ContractType = c.ContractType != null ? c.ContractType.ContractTypeName : "N/A"
                })
                .ToListAsync(cancellationToken);

            var contractByEmployee = ResolveContractByEmployee(contracts, monthEnd);
            var employeeIds = employees.Select(e => e.Id).ToHashSet();

            foreach (var employee in employees)
            {
                if (contractByEmployee.TryGetValue(employee.Id, out var contract))
                {
                    employee.Position = contract.Position;
                    employee.ContractType = contract.ContractType;
                }
            }

            var earliestMonthStart = monthStart.AddMonths(-11);
            var salaries = await _db.EmployeeSalaries
                .AsNoTracking()
                .Where(s =>
                    employeeIds.Contains(s.EmployeeId) &&
                    s.EffectiveDate <= monthEnd &&
                    (!s.EndDate.HasValue || s.EndDate.Value >= earliestMonthStart))
                .Select(s => new SalarySnapshot
                {
                    EmployeeId = s.EmployeeId,
                    BaseSalary = s.BaseSalary,
                    EffectiveDate = s.EffectiveDate.Date,
                    EndDate = s.EndDate.HasValue ? s.EndDate.Value.Date : null
                })
                .ToListAsync(cancellationToken);

            return new DashboardBuildContext
            {
                CompanyId = access.CompanyId,
                CompanyName = access.CompanyName,
                MonthStart = monthStart,
                MonthEnd = monthEnd,
                Employees = employees,
                Contracts = contracts,
                Salaries = salaries
            };
        }

        private DashboardHrVueGlobaleDto BuildVueGlobale(DashboardBuildContext context)
        {
            var totalEmployees = context.Employees.Count;
            var activeEmployees = context.Employees.Count(e => IsEmployeeActive(e.StatusCode));
            var turnover = CalculateTurnover12M(context.Contracts, context.MonthStart, totalEmployees);
            var (femaleCount, maleCount, _, _) = CountGender(context.Employees);
            var knownGenderCount = Math.Max(femaleCount + maleCount, 1);

            var sixMonths = Enumerable.Range(0, 6)
                .Select(offset => context.MonthStart.AddMonths(-5 + offset))
                .ToList();

            var effectifEvolution = sixMonths
                .Select(monthValue => new DashboardHrMonthHeadcountDto
                {
                    Month = monthValue.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    Value = CountHeadcountAtMonthEnd(context, monthValue)
                })
                .ToList();

            var repartitionDepartement = context.Employees
                .GroupBy(e => e.Department)
                .Select(group => new DashboardHrDepartmentCountDto
                {
                    Department = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ToList();

            return new DashboardHrVueGlobaleDto
            {
                Kpis = new DashboardHrVueGlobaleKpisDto
                {
                    EffectifTotal = totalEmployees,
                    MasseSalarialeMad = ResolveGrossAtMonth(context, context.MonthStart),
                    Turnover12mPct = DecimalRound(turnover),
                    Parite = new DashboardHrParityRatioDto
                    {
                        FemalePct = DecimalRound((decimal)femaleCount * 100m / knownGenderCount),
                        MalePct = DecimalRound((decimal)maleCount * 100m / knownGenderCount)
                    }
                },
                EffectifEvolution6M = effectifEvolution,
                RepartitionDepartement = repartitionDepartement
            };
        }

        private DashboardHrMouvementsDto BuildMouvements(DashboardBuildContext context)
        {
            var start = context.MonthStart;
            var end = context.MonthEnd;

            var entries = context.Contracts
                .Where(c => c.StartDate >= start && c.StartDate <= end)
                .OrderBy(c => c.StartDate)
                .ToList();

            var exits = context.Contracts
                .Where(c => c.EndDate.HasValue && c.EndDate.Value >= start && c.EndDate.Value <= end)
                .OrderBy(c => c.EndDate)
                .ToList();

            var employeeById = context.Employees.ToDictionary(e => e.Id, e => e);

            var entryRows = entries.Select(c =>
            {
                employeeById.TryGetValue(c.EmployeeId, out var employee);
                return new DashboardHrMovementRowDto
                {
                    EmployeeId = c.EmployeeId,
                    EmployeeName = employee != null ? employee.FullName : $"Employee #{c.EmployeeId}",
                    Department = employee?.Department ?? "Autres",
                    Position = c.Position,
                    ContractType = c.ContractType,
                    Date = DateOnly.FromDateTime(c.StartDate),
                    Reason = "Nouvelle embauche",
                    MovementType = DashboardHrMovementType.ENTRY
                };
            });

            var exitRows = exits.Select(c =>
            {
                employeeById.TryGetValue(c.EmployeeId, out var employee);
                return new DashboardHrMovementRowDto
                {
                    EmployeeId = c.EmployeeId,
                    EmployeeName = employee != null ? employee.FullName : $"Employee #{c.EmployeeId}",
                    Department = employee?.Department ?? "Autres",
                    Position = c.Position,
                    ContractType = c.ContractType,
                    Date = DateOnly.FromDateTime(c.EndDate!.Value),
                    Reason = "Fin de contrat",
                    MovementType = DashboardHrMovementType.EXIT
                };
            });

            var rows = entryRows.Concat(exitRows)
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.EmployeeName)
                .ToList();

            var exitsDistinctEmployees = exits.Select(e => e.EmployeeId).Distinct().Count();
            var totalEmployees = context.Employees.Count;
            var retention = totalEmployees > 0 ? (decimal)(totalEmployees - exitsDistinctEmployees) * 100m / totalEmployees : 0m;

            return new DashboardHrMouvementsDto
            {
                Summary = new DashboardHrMovementSummaryDto
                {
                    Entrees = entries.Count,
                    Sorties = exits.Count,
                    SoldeNet = entries.Count - exits.Count,
                    RetentionPct = DecimalRound(retention)
                },
                Rows = rows
            };
        }

        private DashboardHrMasseSalarialeDto BuildMasseSalariale(DashboardBuildContext context)
        {
            var monthStarts = Enumerable.Range(0, 12)
                .Select(offset => context.MonthStart.AddMonths(-11 + offset))
                .ToList();

            var monthly = monthStarts
                .Select(monthValue => new DashboardHrMonthAmountDto
                {
                    Month = monthValue.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    ValueMad = DecimalRound(ResolveGrossAtMonth(context, monthValue))
                })
                .ToList();

            var gross = monthly.LastOrDefault()?.ValueMad ?? 0m;
            var net = gross * 0.772m;
            var charges = gross * 0.216m;
            var totalCost = gross + charges;

            var salaryByEmployee = ResolveSalaryByEmployeeAtMonth(context, context.MonthStart);
            var departmentPayroll = context.Employees
                .GroupBy(e => e.Department)
                .Select(group =>
                {
                    var amount = group.Sum(employee => salaryByEmployee.TryGetValue(employee.Id, out var salary) ? salary : 0m);
                    var share = gross > 0 ? amount * 100m / gross : 0m;
                    return new DashboardHrDepartmentPayrollDto
                    {
                        Department = group.Key,
                        Employees = group.Count(),
                        AmountMad = DecimalRound(amount),
                        SharePct = DecimalRound(share)
                    };
                })
                .OrderByDescending(row => row.AmountMad)
                .ToList();

            return new DashboardHrMasseSalarialeDto
            {
                Kpis = new DashboardHrMasseSalarialeKpisDto
                {
                    BrutTotalMad = DecimalRound(gross),
                    NetTotalMad = DecimalRound(net),
                    ChargesPatronalesMad = DecimalRound(charges),
                    CoutTotalEmployeurMad = DecimalRound(totalCost)
                },
                Brut12m = monthly,
                RepartitionDepartement = departmentPayroll
            };
        }

        private DashboardHrPariteDiversiteDto BuildPariteDiversite(DashboardBuildContext context)
        {
            var (femaleCount, maleCount, femaleIds, maleIds) = CountGender(context.Employees);

            var salaryByEmployee = ResolveSalaryByEmployeeAtMonth(context, context.MonthStart);
            var femaleSalaries = context.Employees
                .Where(e => femaleIds.Contains(e.Id) && salaryByEmployee.ContainsKey(e.Id))
                .Select(e => salaryByEmployee[e.Id])
                .ToList();
            var maleSalaries = context.Employees
                .Where(e => maleIds.Contains(e.Id) && salaryByEmployee.ContainsKey(e.Id))
                .Select(e => salaryByEmployee[e.Id])
                .ToList();

            var femaleAvg = femaleSalaries.Count > 0 ? femaleSalaries.Average() : 0m;
            var maleAvg = maleSalaries.Count > 0 ? maleSalaries.Average() : 0m;
            var salaryGap = maleAvg > 0m ? ((femaleAvg - maleAvg) / maleAvg) * 100m : 0m;

            var pariteDepartement = context.Employees
                .GroupBy(e => e.Department)
                .Select(group =>
                {
                    var groupFemale = group.Count(e => IsFemale(e.GenderCode));
                    var groupMale = group.Count(e => IsMale(e.GenderCode));
                    var groupKnown = Math.Max(groupFemale + groupMale, 1);

                    return new DashboardHrPariteDepartmentDto
                    {
                        Department = group.Key,
                        FemaleCount = groupFemale,
                        MaleCount = groupMale,
                        FemalePct = DecimalRound((decimal)groupFemale * 100m / groupKnown)
                    };
                })
                .OrderByDescending(row => row.FemaleCount + row.MaleCount)
                .ToList();

            var hierarchyOrder = new Dictionary<string, int>
            {
                ["Direction"] = 1,
                ["Managers"] = 2,
                ["Cadres"] = 3,
                ["Employes"] = 4
            };

            var pariteNiveauHierarchique = context.Employees
                .GroupBy(e => ClassifyHierarchy(e.Position))
                .Select(group =>
                {
                    var female = group.Count(e => IsFemale(e.GenderCode));
                    var total = group.Count();
                    return new DashboardHrPariteHierarchyDto
                    {
                        Level = group.Key,
                        Total = total,
                        FemaleCount = female,
                        FemalePct = total > 0 ? DecimalRound((decimal)female * 100m / total) : 0m
                    };
                })
                .OrderBy(row => hierarchyOrder.TryGetValue(row.Level, out var rank) ? rank : 999)
                .ToList();

            return new DashboardHrPariteDiversiteDto
            {
                Kpis = new DashboardHrPariteDiversiteKpisDto
                {
                    EffectifFemmes = femaleCount,
                    EffectifHommes = maleCount,
                    EcartSalarialPct = DecimalRound(salaryGap)
                },
                PariteDepartement = pariteDepartement,
                PariteNiveauHierarchique = pariteNiveauHierarchique
            };
        }

        private DashboardHrConformiteSocialeDto BuildConformiteSociale(DashboardBuildContext context)
        {
            var gross = ResolveGrossAtMonth(context, context.MonthStart);

            var cnssSalariale = gross * 0.0429m;
            var cnssPatronale = gross * 0.2109m;
            var amoSalariale = gross * 0.0226m;
            var irRetenu = gross * 0.161m;

            var declarations = new List<DashboardHrDeclarationDto>();

            var monthCode = context.MonthStart.ToString("yyyyMM", CultureInfo.InvariantCulture);
            var nextMonth = context.MonthStart.AddMonths(1);
            var cnssDeadline = new DateOnly(nextMonth.Year, nextMonth.Month, Math.Min(28, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month)));
            var irDeadline = new DateOnly(nextMonth.Year, nextMonth.Month, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));

            declarations.Add(new DashboardHrDeclarationDto
            {
                Type = DashboardHrDeclarationType.CNSS,
                Label = $"Bordereau CNSS - {context.MonthStart:yyyy-MM}",
                AmountMad = DecimalRound(cnssSalariale + cnssPatronale),
                Deadline = cnssDeadline,
                Status = ResolveStatus(cnssDeadline, submitted: false),
                Reference = $"CNSS-{monthCode}"
            });

            declarations.Add(new DashboardHrDeclarationDto
            {
                Type = DashboardHrDeclarationType.AMO,
                Label = $"AMO employeur - {context.MonthStart:yyyy-MM}",
                AmountMad = DecimalRound(amoSalariale),
                Deadline = cnssDeadline,
                Status = ResolveStatus(cnssDeadline, submitted: false),
                Reference = $"AMO-{monthCode}"
            });

            declarations.Add(new DashboardHrDeclarationDto
            {
                Type = DashboardHrDeclarationType.IR,
                Label = $"Versement IR DGI - {context.MonthStart:yyyy-MM}",
                AmountMad = DecimalRound(irRetenu),
                Deadline = irDeadline,
                Status = ResolveStatus(irDeadline, submitted: DateOnly.FromDateTime(DateTime.UtcNow.Date) > irDeadline),
                Reference = $"IR-{monthCode}"
            });

            return new DashboardHrConformiteSocialeDto
            {
                Kpis = new DashboardHrConformiteKpisDto
                {
                    CnssSalarialeMad = DecimalRound(cnssSalariale),
                    CnssPatronaleMad = DecimalRound(cnssPatronale),
                    AmoSalarialeMad = DecimalRound(amoSalariale),
                    IrRetenuSourceMad = DecimalRound(irRetenu)
                },
                Declarations = declarations
            };
        }

        private async Task<CompanyAccessResult> ResolveCompanyAccessAsync(int? requestedCompanyId, CancellationToken cancellationToken)
        {
            var userId = ReadCurrentUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("Utilisateur non authentifie.");
            }

            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive && u.DeletedAt == null, cancellationToken);

            if (user?.Employee == null)
            {
                throw new UnauthorizedAccessException("Utilisateur non associe a un employe.");
            }

            var userCompanyId = user.Employee.CompanyId;
            var targetCompanyId = requestedCompanyId ?? userCompanyId;

            var targetCompany = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == targetCompanyId && c.DeletedAt == null, cancellationToken);

            if (targetCompany == null)
            {
                throw new KeyNotFoundException("Societe cible introuvable.");
            }

            if (targetCompanyId == userCompanyId)
            {
                return new CompanyAccessResult(targetCompany.Id, targetCompany.CompanyName);
            }

            var managingCompany = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == userCompanyId && c.DeletedAt == null, cancellationToken);

            var canAccessAsExpert = managingCompany != null &&
                                    managingCompany.IsCabinetExpert &&
                                    targetCompany.ManagedByCompanyId == managingCompany.Id;

            if (!canAccessAsExpert)
            {
                throw new UnauthorizedAccessException("Acces refuse a la societe demandee.");
            }

            return new CompanyAccessResult(targetCompany.Id, targetCompany.CompanyName);
        }

        private static DateTime ParseMonth(string? month)
        {
            if (string.IsNullOrWhiteSpace(month))
            {
                var now = DateTime.UtcNow;
                return new DateTime(now.Year, now.Month, 1);
            }

            if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                throw new ArgumentException("Le format month doit etre yyyy-MM.", nameof(month));
            }

            return new DateTime(parsed.Year, parsed.Month, 1);
        }

        private static DateTime EndOfMonth(DateTime monthStart) => monthStart.AddMonths(1).AddDays(-1);

        private static Dictionary<int, ContractSnapshot> ResolveContractByEmployee(IEnumerable<ContractSnapshot> contracts, DateTime monthEnd)
        {
            return contracts
                .Where(c => c.StartDate <= monthEnd && (!c.EndDate.HasValue || c.EndDate.Value >= monthEnd))
                .GroupBy(c => c.EmployeeId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(c => c.StartDate).First());
        }

        private static bool IsEmployeeActive(string statusCode)
        {
            var normalized = (statusCode ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return true;
            }

            if (ActiveStatusTokens.Any(token => normalized.Contains(token)))
            {
                return true;
            }

            if (InactiveStatusTokens.Any(token => normalized.Contains(token)))
            {
                return false;
            }

            return true;
        }

        private static decimal CalculateTurnover12M(IEnumerable<ContractSnapshot> contracts, DateTime monthStart, int currentHeadcount)
        {
            if (currentHeadcount <= 0)
            {
                return 0m;
            }

            var from = monthStart.AddMonths(-11);
            var to = EndOfMonth(monthStart);
            var exits = contracts.Count(c => c.EndDate.HasValue && c.EndDate.Value >= from && c.EndDate.Value <= to);
            return (decimal)exits * 100m / currentHeadcount;
        }

        private static int CountHeadcountAtMonthEnd(DashboardBuildContext context, DateTime monthStart)
        {
            if (context.Contracts.Count == 0)
            {
                return context.Employees.Count;
            }

            var monthEnd = EndOfMonth(monthStart);
            var count = context.Contracts
                .Where(c => c.StartDate <= monthEnd && (!c.EndDate.HasValue || c.EndDate.Value >= monthEnd))
                .Select(c => c.EmployeeId)
                .Distinct()
                .Count();

            return count;
        }

        private static decimal ResolveGrossAtMonth(DashboardBuildContext context, DateTime monthStart)
        {
            var salaryByEmployee = ResolveSalaryByEmployeeAtMonth(context, monthStart);
            return salaryByEmployee.Values.Sum();
        }

        private static Dictionary<int, decimal> ResolveSalaryByEmployeeAtMonth(DashboardBuildContext context, DateTime monthStart)
        {
            var monthEnd = EndOfMonth(monthStart);
            return context.Salaries
                .Where(s => s.EffectiveDate <= monthEnd && (!s.EndDate.HasValue || s.EndDate.Value >= monthStart))
                .GroupBy(s => s.EmployeeId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(s => s.EffectiveDate).First().BaseSalary);
        }

        private static (int femaleCount, int maleCount, HashSet<int> femaleIds, HashSet<int> maleIds) CountGender(IEnumerable<EmployeeSnapshot> employees)
        {
            var femaleIds = new HashSet<int>();
            var maleIds = new HashSet<int>();

            foreach (var employee in employees)
            {
                if (IsFemale(employee.GenderCode))
                {
                    femaleIds.Add(employee.Id);
                }
                else if (IsMale(employee.GenderCode))
                {
                    maleIds.Add(employee.Id);
                }
            }

            return (femaleIds.Count, maleIds.Count, femaleIds, maleIds);
        }

        private static bool IsFemale(string? code)
        {
            var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
            return normalized.StartsWith("f") || normalized.Contains("fem") || normalized.Contains("female") || normalized.Contains("woman");
        }

        private static bool IsMale(string? code)
        {
            var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
            return normalized.StartsWith("m") || normalized.Contains("mas") || normalized.Contains("male") || normalized.Contains("hom") || normalized.Contains("man");
        }

        private static string ClassifyHierarchy(string? position)
        {
            var value = (position ?? string.Empty).Trim().ToLowerInvariant();

            if (value.Contains("direction") || value.Contains("directeur") || value.Contains("director"))
            {
                return "Direction";
            }

            if (value.Contains("manager") || value.Contains("chef") || value.Contains("lead"))
            {
                return "Managers";
            }

            if (value.Contains("dev") || value.Contains("engineer") || value.Contains("analyst") || value.Contains("finance") || value.Contains("qa") || value.Contains("rh"))
            {
                return "Cadres";
            }

            return "Employes";
        }

        private static DashboardHrDeclarationStatus ResolveStatus(DateOnly deadline, bool submitted)
        {
            if (submitted)
            {
                return DashboardHrDeclarationStatus.SUBMITTED;
            }

            return DateOnly.FromDateTime(DateTime.UtcNow.Date) > deadline
                ? DashboardHrDeclarationStatus.OVERDUE
                : DashboardHrDeclarationStatus.PENDING;
        }

        private int? ReadCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return null;
            }

            var rawValue = user.Claims.FirstOrDefault(claim => claim.Type == "uid")?.Value
                           ?? user.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;

            return int.TryParse(rawValue, out var userId) ? userId : null;
        }

        private static decimal DecimalRound(decimal value) => Math.Round(value, 2);

        private sealed class DashboardBuildContext
        {
            public int CompanyId { get; init; }
            public required string CompanyName { get; init; }
            public required DateTime MonthStart { get; init; }
            public required DateTime MonthEnd { get; init; }
            public required List<EmployeeSnapshot> Employees { get; init; }
            public required List<ContractSnapshot> Contracts { get; init; }
            public required List<SalarySnapshot> Salaries { get; init; }
        }

        private sealed record CompanyAccessResult(int CompanyId, string CompanyName);

        private sealed class EmployeeSnapshot
        {
            public int Id { get; init; }
            public required string FirstName { get; init; }
            public required string LastName { get; init; }
            public required string Department { get; init; }
            public required string StatusCode { get; init; }
            public required string GenderCode { get; init; }
            public string Position { get; set; } = "Non assigne";
            public string ContractType { get; set; } = "N/A";
            public string FullName => $"{FirstName} {LastName}".Trim();
        }

        private sealed class ContractSnapshot
        {
            public int EmployeeId { get; init; }
            public required DateTime StartDate { get; init; }
            public DateTime? EndDate { get; init; }
            public required string Position { get; init; }
            public required string ContractType { get; init; }
        }

        private sealed class SalarySnapshot
        {
            public int EmployeeId { get; init; }
            public decimal BaseSalary { get; init; }
            public required DateTime EffectiveDate { get; init; }
            public DateTime? EndDate { get; init; }
        }
    }
}
