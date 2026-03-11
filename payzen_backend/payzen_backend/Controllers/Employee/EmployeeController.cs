using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Authorization;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Dashboard.Dtos;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Models.Event;
using payzen_backend.Models.Permissions;
using payzen_backend.Models.Permissions.Dtos;
using payzen_backend.Models.Users;
using payzen_backend.Services;
using System;
using System.Globalization;
using static payzen_backend.Models.Permissions.PermissionsConstants;

namespace payzen_backend.Controllers.Employee
{
    [Route("api/employee")]
    [ApiController]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PasswordGeneratorService _passwordGenerator;
        private readonly EmployeeEventLogService _eventLogService;

        public EmployeeController(
            AppDbContext db,
            PasswordGeneratorService passwordGenerator,
            EmployeeEventLogService eventLogService)
        {
            _db = db;
            _passwordGenerator = passwordGenerator;
            _eventLogService = eventLogService;
        }

        /// <summary>
        /// Récupère l'employé lié à l'utilisateur authentifié.
        /// Utilisé par le frontend pour résoudre l'ID employé d'une session sans employee_id dans le token.
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.GetUserId();
            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.DeletedAt == null);

            if (user?.Employee == null)
                return NotFound(new { message = "Aucun dossier employé lié à ce compte." });

            return Ok(new
            {
                employeeId = user.Employee.Id,
                firstName  = user.Employee.FirstName,
                lastName   = user.Employee.LastName
            });
        }

        /// <summary>
        /// Récupère tous les employés actifs
        /// </summary>
        [HttpGet("summary")]
        //[HasPermission(READ_EMPLOYEES)]
        public async Task<ActionResult<DashboardResponseDto>> GetAll([FromQuery] int? companyId = null)
        {
            // Base query (filtrage optionnel par companyId)
            var baseQuery = _db.Employees
                .AsNoTracking()
                .Where(e => e.DeletedAt == null);

            if (companyId.HasValue)
                baseQuery = baseQuery.Where(e => e.CompanyId == companyId.Value);

            // Comptages (scopés par companyId si fourni)
            var totalEmployees = await baseQuery.CountAsync();
            var activeEmployees = await baseQuery
                .Where(e => e.Status != null && e.Status.Code == "Active")
                .CountAsync();

            // Projection server-side : retourne directement departement name et status name (pas de mapping)
            var employees = await baseQuery
                .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                .Select(e => new EmployeeDashboardItemDto
                {
                    Id = e.Id.ToString(),
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Department = e.Departement != null ? e.Departement.DepartementName : "Non assigné",
                    statuses = e.Status != null ? e.Status.Code : string.Empty,
                    Manager = e.Manager != null ? (e.Manager.FirstName + " " + e.Manager.LastName) : null,
                    MissingDocuments = _db.EmployeeDocuments.Count(d => d.EmployeeId == e.Id && d.DeletedAt == null && string.IsNullOrEmpty(d.FilePath)),
                    Position = _db.EmployeeContracts
                        .Where(c => c.EmployeeId == e.Id && c.DeletedAt == null && c.EndDate == null)
                        .OrderByDescending(c => c.StartDate)
                        .Select(c => c.JobPosition.Name)
                        .FirstOrDefault() ?? "Non assigné",
                    StartDate = _db.EmployeeContracts
                        .Where(c => c.EmployeeId == e.Id && c.DeletedAt == null && c.EndDate == null)
                        .OrderByDescending(c => c.StartDate)
                        .Select(c => c.StartDate.ToString("yyyy-MM-dd"))
                        .FirstOrDefault() ?? string.Empty,
                    ContractType = _db.EmployeeContracts
                        .Where(c => c.EmployeeId == e.Id && c.DeletedAt == null && c.EndDate == null)
                        .OrderByDescending(c => c.StartDate)
                        .Select(c => c.ContractType.ContractTypeName)
                        .FirstOrDefault() ?? string.Empty
                })
                .ToListAsync();

            var response = new DashboardResponseDto
            {
                TotalEmployees = totalEmployees,
                ActiveEmployees = activeEmployees,
                Employees = employees
            };

            return Ok(response);
        }
        /// <summary>
        /// Récupère les détails complets d'un employé par ID
        /// </summary>
        [HttpGet("{id}/details")]
        //[HasPermission(VIEW_EMPLOYEE)]
        public async Task<ActionResult<EmployeeDetailDto>> GetEmployeeDetails(int id)
        {
            var userId = User.GetUserId();

            var employee = await _db.Employees
                .AsNoTracking()
                .Where(e => e.Id == id && e.DeletedAt == null)
                .Include(e => e.Company)
                .Include(e => e.Manager)
                .Include(e => e.Departement)
                .Include(e => e.Status)
                .Include(e => e.MaritalStatus)
                .Include(e => e.Category)
                .Include(e => e.Gender)
                .Include(e => e.Addresses!.Where(a => a.DeletedAt == null))
                    .ThenInclude(e => e.City)
                    .ThenInclude(e => e.Country)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.JobPosition)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.ContractType)
                .Include(e => e.Salaries!.Where(s => s.DeletedAt == null && s.EndDate == null))
                    .ThenInclude(s => s.Components!.Where(c => c.DeletedAt == null && c.EndDate == null))
                .FirstOrDefaultAsync();

            if (employee == null)
                return NotFound(new { Message = "Employé non trouvé" });

            // ===== VÉRIFIER LE RÔLE DE L'UTILISATEUR ACTUEL =====
            var currentUser = await _db.Users
                .AsNoTracking()
                .Include(u => u.UsersRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            bool isRH = currentUser?.UsersRoles?.Any(ur =>
                ur.Role.Name.Equals("RH", StringComparison.OrdinalIgnoreCase)
            ) ?? false;

            Console.WriteLine($"isRH is : {isRH}");

            bool isAdmin = currentUser?.UsersRoles?.Any(ur =>
                ur.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ) ?? false;

            Console.WriteLine($"IsAdmin is : {isAdmin}");
            // Récupérer le contrat actif
            var activeContract = employee.Contracts?
                .Where(c => c.DeletedAt == null && c.EndDate == null)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefault();

            // Récupérer le salaire actif
            var activeSalary = employee.Salaries?
                .Where(s => s.DeletedAt == null && s.EndDate == null)
                .OrderByDescending(s => s.EffectiveDate)
                .FirstOrDefault();

            // Récupérer l'adresse active (la plus récente)
            var activeAddress = employee.Addresses?
                .Where(a => a.DeletedAt == null)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault();

            // Calculer les composants salariaux
            var salaryComponents = activeSalary?.Components?
                .Where(c => c.DeletedAt == null && c.EndDate == null)
                .Select(c => new SalaryComponentDto
                {
                    ComponentName = c.ComponentType,
                    Amount = c.Amount,
                    IsTaxable = c.IsTaxable == true // Explicit boolean conversion
                })
                .ToList() ?? new List<SalaryComponentDto>();

            // Calculer le salaire total
            decimal baseSalary = activeSalary?.BaseSalary ?? 0;
            decimal totalComponents = salaryComponents.Sum(c => c.Amount);
            decimal totalSalary = baseSalary + totalComponents;

            // Get the Employee Category
            var category = await _db.EmployeeCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(cat => cat.Id == employee.CategoryId);

            // Get the gendre Name
            var gender = await _db.Genders
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == employee.GenderId);

            // ===== RÉCUPÉRER L'HISTORIQUE DES MODIFICATIONS =====
            var eventLogs = await _db.EmployeeEventLogs
                .Where(e => e.employeeId == id)
                .OrderByDescending(e => e.createdAt)
                .ToListAsync();

            // Créer une liste pour stocker les événements formatés
            var formattedEvents = new List<object>();

            foreach (var log in eventLogs)
            {
                // Récupérer l'utilisateur qui a effectué la modification avec son rôle
                var modifier = await _db.Users
                    .AsNoTracking()
                    .Include(u => u.Employee)
                    .Include(u => u.UsersRoles)
                        .ThenInclude(ur => ur.Role)
                    .Where(u => u.Id == log.createdBy)
                    .Select(u => new
                    {
                        FirstName = u.Employee != null ? u.Employee.FirstName : "Système",
                        LastName = u.Employee != null ? u.Employee.LastName : "",
                        RoleName = u.UsersRoles != null && u.UsersRoles.Any()
                            ? u.UsersRoles.First().Role.Name
                            : "Système"
                    })
                    .FirstOrDefaultAsync();

                string modifierName = modifier != null
                    ? $"{modifier.FirstName} {modifier.LastName}".Trim()
                    : "Système";

                string modifierRole = modifier?.RoleName ?? "Système";

                // Construire l'événement selon son type
                object eventData = null;

                switch (log.eventName)
                {
                    case EmployeeEventLogService.EventNames.SalaryUpdated:
                        eventData = new
                        {
                            Type = "salary_increase",
                            Title = "Augmentation de salaire",
                            Date = log.createdAt.ToString("dd/MM/yyyy"),
                            Description = isRH
                                ? $"Salaire de base: {log.oldValue} MAD → {log.newValue} MAD"
                                : "Modification du salaire de base",
                            Details = isRH ? new
                            {
                                OldSalary = log.oldValue,
                                NewSalary = log.newValue,
                                Currency = "MAD"
                            } : null,
                            ModifiedBy = new
                            {
                                Name = modifierName,
                                Role = modifierRole
                            },
                            Timestamp = log.createdAt
                        };
                        break;

                    case EmployeeEventLogService.EventNames.JobPositionChanged:
                        eventData = new
                        {
                            Type = "promotion",
                            Title = "Promotion",
                            Date = log.createdAt.ToString("dd/MM/yyyy"),
                            Description = isRH
                                ? $"{log.oldValue} → {log.newValue}"
                                : "Changement de poste",
                            Details = isRH ? new
                            {
                                OldPosition = log.oldValue,
                                NewPosition = log.newValue
                            } : null,
                            ModifiedBy = new
                            {
                                Name = modifierName,
                                Role = modifierRole
                            },
                            Timestamp = log.createdAt
                        };
                        break;

                    case EmployeeEventLogService.EventNames.ContractCreated:
                        eventData = new
                        {
                            Type = "contract_created",
                            Title = "Nouveau contrat",
                            Date = log.createdAt.ToString("dd/MM/yyyy"),
                            Description = isRH
                                ? $"Contrat créé: {log.newValue}"
                                : "Nouveau contrat créé",
                            Details = isRH ? new
                            {
                                ContractInfo = log.newValue
                            } : null,
                            ModifiedBy = new
                            {
                                Name = modifierName,
                                Role = modifierRole
                            },
                            Timestamp = log.createdAt
                        };
                        break;

                    case EmployeeEventLogService.EventNames.ContractTerminated:
                        eventData = new
                        {
                            Type = "contract_terminated",
                            Title = "Fin de contrat",
                            Date = log.createdAt.ToString("dd/MM/yyyy"),
                            Description = isRH
                                ? $"Contrat {log.oldValue} terminé le {log.newValue}"
                                : "Fin de contrat",
                            Details = isRH ? new
                            {
                                ContractInfo = log.oldValue,
                                EndDate = log.newValue
                            } : null,
                            ModifiedBy = new
                            {
                                Name = modifierName,
                                Role = modifierRole
                            },
                            Timestamp = log.createdAt
                        };
                        break;

                    case EmployeeEventLogService.EventNames.StatusChanged:
                        var statusTitle = "Changement de statut";
                        var statusDescription = "Statut modifié";

                        // Détection de fin de période d'essai
                        if (log.newValue != null &&
                            (log.newValue.Contains("Actif", StringComparison.OrdinalIgnoreCase) ||
                             log.newValue.Contains("Active", StringComparison.OrdinalIgnoreCase)) &&
                            log.oldValue != null &&
                            (log.oldValue.Contains("Essai", StringComparison.OrdinalIgnoreCase) ||
                             log.oldValue.Contains("Trial", StringComparison.OrdinalIgnoreCase)))
                        {
                            statusTitle = "Fin de période d'essai";
                            statusDescription = "Période d'essai validée avec succès";
                        }
                        else if (isRH)
                        {
                            statusDescription = $"{log.oldValue} → {log.newValue}";
                        }

                        eventData = new
                        {
                            Type = "status_change",
                            Title = statusTitle,
                            Date = log.createdAt.ToString("dd/MM/yyyy"),
                            Description = statusDescription,
                            Details = isRH ? new
                            {
                                OldStatus = log.oldValue,
                                NewStatus = log.newValue
                            } : null,
                            ModifiedBy = new
                            {
                                Name = modifierName,
                                Role = modifierRole
                            },
                            Timestamp = log.createdAt
                        };
                        break;

                    case EmployeeEventLogService.EventNames.DepartmentChanged:
                        eventData = new
                        {
                            Type = "department_change",
                            Title = "Changement de département",
                            Date = log.createdAt.ToString("dd/MM/yyyy"),
                            Description = isRH
                                ? $"{log.oldValue} → {log.newValue}"
                                : "Département modifié",
                            Details = isRH ? new
                            {
                                OldDepartment = log.oldValue,
                                NewDepartment = log.newValue
                            } : null,
                            ModifiedBy = new
                            {
                                Name = modifierName,
                                Role = modifierRole
                            },
                            Timestamp = log.createdAt
                        };
                        break;

                    case EmployeeEventLogService.EventNames.ManagerChanged:
                        eventData = new
                        {
                            Type = "manager_change",
                            Title = "Changement de manager",
                            Date = log.createdAt.ToString("dd/MM/yyyy"),
                            Description = isRH
                                ? $"{log.oldValue ?? "Aucun"} → {log.newValue}"
                                : "Manager modifié",
                            Details = isRH ? new
                            {
                                OldManager = log.oldValue,
                                NewManager = log.newValue
                            } : null,
                            ModifiedBy = new
                            {
                                Name = modifierName,
                                Role = modifierRole
                            },
                            Timestamp = log.createdAt
                        };
                        break;

                    case EmployeeEventLogService.EventNames.AddressUpdated:
                    case EmployeeEventLogService.EventNames.AddressCreated:
                        eventData = new
                        {
                            Type = log.eventName == EmployeeEventLogService.EventNames.AddressCreated
                                ? "address_created"
                                : "address_updated",
                            Title = log.eventName == EmployeeEventLogService.EventNames.AddressCreated
                                ? "Ajout d'adresse"
                                : "Modification d'adresse",
                            Date = log.createdAt.ToString("dd/MM/yyyy"),
                            Description = isRH
                                ? (log.oldValue != null ? $"{log.oldValue} → {log.newValue}" : log.newValue)
                                : "Adresse modifiée",
                            Details = isRH ? new
                            {
                                OldAddress = log.oldValue,
                                NewAddress = log.newValue
                            } : null,
                            ModifiedBy = new
                            {
                                Name = modifierName,
                                Role = modifierRole
                            },
                            Timestamp = log.createdAt
                        };
                        break;

                    // Cas par défaut pour les autres événements
                    default:
                        var eventTitle = log.eventName switch
                        {
                            EmployeeEventLogService.EventNames.FirstNameChanged => "Modification du prénom",
                            EmployeeEventLogService.EventNames.LastNameChanged => "Modification du nom",
                            EmployeeEventLogService.EventNames.EmailChanged => "Modification de l'email",
                            EmployeeEventLogService.EventNames.PhoneChanged => "Modification du téléphone",
                            EmployeeEventLogService.EventNames.CinNumberChanged => "Modification du CIN",
                            EmployeeEventLogService.EventNames.DateOfBirthChanged => "Modification de la date de naissance",
                            EmployeeEventLogService.EventNames.GenderChanged => "Modification du genre",
                            EmployeeEventLogService.EventNames.MaritalStatusChanged => "Modification du statut marital",
                            EmployeeEventLogService.EventNames.NationalityChanged => "Modification de la nationalité",
                            EmployeeEventLogService.EventNames.EducationLevelChanged => "Modification du niveau d'éducation",
                            EmployeeEventLogService.EventNames.CnssNumberChanged => "Modification du numéro CNSS",
                            EmployeeEventLogService.EventNames.CimrNumberChanged => "Modification du numéro CIMR",
                            EmployeeEventLogService.EventNames.ContractTypeChanged => "Modification du type de contrat",
                            _ => "Modification"
                        };

                        eventData = new
                        {
                            Type = "general_update",
                            Title = eventTitle,
                            Date = log.createdAt.ToString("dd/MM/yyyy"),
                            Description = isRH
                                ? $"{log.oldValue ?? "N/A"} → {log.newValue ?? "N/A"}"
                                : "Informations modifiées",
                            Details = isRH ? new
                            {
                                OldValue = log.oldValue,
                                NewValue = log.newValue
                            } : null,
                            ModifiedBy = new
                            {
                                Name = modifierName,
                                Role = modifierRole
                            },
                            Timestamp = log.createdAt
                        };
                        break;
                }

                if (eventData != null)
                {
                    formattedEvents.Add(eventData);
                }
            }

            var result = new EmployeeDetailDto
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                CinNumber = employee.CinNumber,
                MaritalStatusName = employee.MaritalStatus?.Code,
                DateOfBirth = employee.DateOfBirth,
                StatusName = employee.Status?.Code,
                Email = employee.Email,
                Phone = employee.Phone,
                CountryPhoneCode = activeAddress?.City?.Country?.CountryPhoneCode,
                GenderId = employee.GenderId,
                GenderName = gender?.Code,

                // Adresse
                Address = activeAddress != null ? new EmployeeAddressDto
                {
                    AddressLine1 = activeAddress.AddressLine1,
                    AddressLine2 = activeAddress.AddressLine2,
                    ZipCode = activeAddress.ZipCode,
                    CityName = activeAddress.City?.CityName ?? "",
                    CountryName = activeAddress.City?.Country?.CountryName,
                } : null,

                // Informations de contrat
                JobPositionName = activeContract?.JobPosition?.Name,
                ManagerName = employee.Manager != null
                    ? $"{employee.Manager.FirstName} {employee.Manager.LastName}"
                    : null,
                ContractStartDate = activeContract?.StartDate,
                ContractTypeName = activeContract?.ContractType?.ContractTypeName,
                departments = employee.Departement != null ? employee.Departement.DepartementName : null,

                // Informations salariales
                BaseSalary = baseSalary,
                SalaryComponents = salaryComponents,
                TotalSalary = totalSalary,

                // Numéro CNSS, CIMR, PrivateAssurance
                cnss = employee.CnssNumber,
                cimr = employee.CimrNumber,
                cimrEmployeeRate = employee.CimrEmployeeRate,
                cimrCompanyRate = employee.CimrCompanyRate,
                hasPrivateInsurance = employee.HasPrivateInsurance,
                privateInsuranceNumber = employee.PrivateInsuranceNumber,
                privateInsuranceRate = employee.PrivateInsuranceRate,
                disableAmo = employee.DisableAmo,


                // Category
                CategoryId = employee.CategoryId,
                CategoryName = category != null ? category.Name : null,
                CreatedAt = employee.CreatedAt.DateTime,

                Events = formattedEvents
            };

            return Ok(result);
        }

        /// <summary>
        /// Récupère un employé par ID
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission(VIEW_EMPLOYEE)]
        public async Task<ActionResult<EmployeeReadDto>> GetById(int id)
        {
            var employee = await _db.Employees
                .AsNoTracking()
                .Where(e => e.DeletedAt == null)
                .Include(e => e.Company)
                .Include(e => e.Manager)
                .Include(e => e.Departement)
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound(new { Message = "Employé non trouvé" });

            var result = new EmployeeReadDto
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                CinNumber = employee.CinNumber,
                DateOfBirth = employee.DateOfBirth,
                Phone = employee.Phone,
                Email = employee.Email,
                CompanyId = employee.CompanyId,
                CompanyName = employee.Company?.CompanyName ?? "",
                DepartementId = employee.DepartementId,
                DepartementName = employee.Departement?.DepartementName,
                ManagerId = employee.ManagerId,
                ManagerName = employee.Manager != null ? $"{employee.Manager.FirstName} {employee.Manager.LastName}" : null,
                StatusId = employee.StatusId,
                GenderId = employee.GenderId,
                NationalityId = employee.NationalityId,
                EducationLevelId = employee.EducationLevelId,
                MaritalStatusId = employee.MaritalStatusId,
                CategoryId = employee.CategoryId,
                CategoryName = employee.Category?.Name,
                CreatedAt = employee.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Récupère tous les employés d'une société
        /// </summary>
        [HttpGet("company/{companyId}")]
        //[HasPermission(VIEW_COMPANY_EMPLOYEES)]
        public async Task<ActionResult<DashboardResponseDto>> GetByCompanyId(int companyId)
        {
            var companyExists = await _db.Companies.AnyAsync(c => c.Id == companyId && c.DeletedAt == null);
            if (!companyExists)
                return NotFound(new { Message = "Société non trouvée" });

            // Base query scoped to company
            var baseQuery = _db.Employees
                .AsNoTracking()
                .Where(e => e.CompanyId == companyId && e.DeletedAt == null);

            // Comptages
            var totalEmployees = await baseQuery.CountAsync();
            var activeEmployees = await baseQuery
                .Where(e => e.Status != null && e.Status.Code == "Active")
                .CountAsync();

            // Projection server-side vers EmployeeDashboardItemDto
            var employees = await baseQuery
                .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                .Select(e => new EmployeeDashboardItemDto
                {
                    Id = e.Id.ToString(),
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Department = e.Departement != null ? e.Departement.DepartementName : "Non assigné",
                    statuses = e.Status != null ? e.Status.Code : string.Empty,
                    Manager = e.Manager != null ? (e.Manager.FirstName + " " + e.Manager.LastName) : null,
                    MissingDocuments = _db.EmployeeDocuments.Count(d => d.EmployeeId == e.Id && d.DeletedAt == null && string.IsNullOrEmpty(d.FilePath)),
                    Position = _db.EmployeeContracts
                        .Where(c => c.EmployeeId == e.Id && c.DeletedAt == null && c.EndDate == null)
                        .OrderByDescending(c => c.StartDate)
                        .Select(c => c.JobPosition.Name)
                        .FirstOrDefault() ?? "Non assigné",
                    StartDate = _db.EmployeeContracts
                        .Where(c => c.EmployeeId == e.Id && c.DeletedAt == null && c.EndDate == null)
                        .OrderByDescending(c => c.StartDate)
                        .Select(c => c.StartDate.ToString("yyyy-MM-dd"))
                        .FirstOrDefault() ?? string.Empty,
                    ContractType = _db.EmployeeContracts
                        .Where(c => c.EmployeeId == e.Id && c.DeletedAt == null && c.EndDate == null)
                        .OrderByDescending(c => c.StartDate)
                        .Select(c => c.ContractType.ContractTypeName)
                        .FirstOrDefault() ?? string.Empty
                })
                .ToListAsync();

            // Remplir la liste globale des statuses utilisée côté front (distinct, non vides)
            var distinctStatuses = employees
                .Select(x => x.statuses)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var response = new DashboardResponseDto
            {
                TotalEmployees = totalEmployees,
                ActiveEmployees = activeEmployees,
                Employees = employees,
                statuses = distinctStatuses
            };

            return Ok(response);
        }

        /// <summary>
        /// Récupère tous les employés d'un département
        /// </summary>
        [HttpGet("departement/{departementId}")]
        //[HasPermission(VIEW_COMPANY_EMPLOYEES)]
        public async Task<ActionResult<IEnumerable<EmployeeReadDto>>> GetByDepartementId(int departementId)
        {
            var departementExists = await _db.Departement.AnyAsync(d => d.Id == departementId && d.DeletedAt == null);
            if (!departementExists)
                return NotFound(new { Message = "Département non trouvé" });

            var employees = await _db.Employees
                .AsNoTracking()
                .Where(e => e.DepartementId == departementId && e.DeletedAt == null)
                .Include(e => e.Company)
                .Include(e => e.Manager)
                .Include(e => e.Departement)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();

            var result = employees.Select(e => new EmployeeReadDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                CinNumber = e.CinNumber,
                DateOfBirth = e.DateOfBirth,
                Phone = e.Phone,
                Email = e.Email,
                CompanyId = e.CompanyId,
                CompanyName = e.Company?.CompanyName ?? "",
                DepartementId = e.DepartementId,
                DepartementName = e.Departement?.DepartementName,
                ManagerId = e.ManagerId,
                ManagerName = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
                StatusId = e.StatusId,
                GenderId = e.GenderId,
                //NationalityId = e.NationalityId,
                EducationLevelId = e.EducationLevelId,
                MaritalStatusId = e.MaritalStatusId,
                CreatedAt = e.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Récupère tous les subordonnés d'un manager
        /// </summary>
        [HttpGet("manager/{managerId}/subordinates")]
        //[HasPermission(VIEW_SUBORDINATES)]
        public async Task<ActionResult<IEnumerable<EmployeeReadDto>>> GetSubordinates(int managerId)
        {
            var managerExists = await _db.Employees.AnyAsync(e => e.Id == managerId && e.DeletedAt == null);
            if (!managerExists)
                return NotFound(new { Message = "Manager non trouvé" });

            var employees = await _db.Employees
                .AsNoTracking()
                .Where(e => e.ManagerId == managerId && e.DeletedAt == null)
                .Include(e => e.Company)
                .Include(e => e.Departement)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();

            var result = employees.Select(e => new EmployeeReadDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                CinNumber = e.CinNumber,
                DateOfBirth = e.DateOfBirth,
                Phone = e.Phone,
                Email = e.Email,
                CompanyId = e.CompanyId,
                CompanyName = e.Company?.CompanyName ?? "",
                DepartementId = e.DepartementId,
                DepartementName = e.Departement?.DepartementName,
                ManagerId = e.ManagerId,
                ManagerName = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
                StatusId = e.StatusId,
                GenderId = e.GenderId,
                //NationalityId = e.NationalityId,
                EducationLevelId = e.EducationLevelId,
                MaritalStatusId = e.MaritalStatusId,
                CreatedAt = e.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Crée un nouvel employé et un compte utilisateur associé
        /// </summary>
        [HttpPost]
        //[HasPermission(CREATE_EMPLOYEE)]
        public async Task<ActionResult<EmployeeReadDto>> Create([FromBody] EmployeeCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // Récupérer l'utilisateur authentifié pour obtenir son CompanyId
            var currentUser = await _db.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.DeletedAt == null);

            if (currentUser?.Employee == null)
                return BadRequest(new { Message = "L'utilisateur n'est pas associé à un employé" });

            // Déterminer companyId cible (fourni ou celui de l'utilisateur)
            var companyId = dto.CompanyId ?? currentUser.Employee.CompanyId;

            // Charger la société cible pour vérifications (ManagedByCompanyId)
            var targetCompany = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId && c.DeletedAt == null);

            if (targetCompany == null)
                return NotFound(new { Message = "Société non trouvée" });

            // Autorisation : si l'utilisateur tente de créer dans une autre société
            // autoriser si et seulement si la société cible est gérée par la société de l'utilisateur
            // (cabinet comptable scenario)
            if (dto.CompanyId.HasValue && dto.CompanyId.Value != currentUser.Employee.CompanyId)
            {
                if (targetCompany.ManagedByCompanyId != currentUser.Employee.CompanyId)
                {
                    // L'utilisateur n'est pas manager de la société cible
                    return Forbid();
                }

                // Optionnel : vérifier que la société de l'utilisateur est bien un cabinet comptable
                var managerCompany = await _db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == currentUser.Employee.CompanyId && c.DeletedAt == null);
                if (managerCompany == null || !managerCompany.IsCabinetExpert)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        Code = "COMPANY_NOT_AUTHORIZED",
                        Message = "La société de l'utilisateur n'est pas un cabinet comptable autorisé"
                    });
                }
            }

            // Vérifier que le département existe et appartient à la bonne société
            // Vérifier si expert comptable, le département doit appartenir à la société de l'utilisateur
            if (dto.DepartementId.HasValue)
            {
                var departement = await _db.Departement
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == dto.DepartementId && d.DeletedAt == null);

                if (departement == null)
                    return NotFound(new { Message = "Département non trouvé" });

                // Autoriser le département de la société cible OU de la société du cabinet
                var allowedCompanyIds = new List<int> { companyId };
                if (companyId != currentUser.Employee.CompanyId)
                    allowedCompanyIds.Add(currentUser.Employee.CompanyId);

                if (!allowedCompanyIds.Contains(departement.CompanyId))
                    return BadRequest(new { Message = "Le département ne correspond pas à la société spécifiée" });
            }

            // Vérifier que le JobPosition existe et appartient à la bonne société (si fourni)
            if (dto.JobPositionId.HasValue)
            {
                var jobPosition = await _db.JobPositions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(jp => jp.Id == dto.JobPositionId && jp.DeletedAt == null);

                if (jobPosition == null)
                    return NotFound(new { Message = "Poste de travail non trouvé" });

                // Autoriser le poste de travail de la société cible OU de la société du cabinet
               var allowedCompanyIds = new List<int> { companyId };
               if (companyId != currentUser.Employee.CompanyId)
                    allowedCompanyIds.Add(currentUser.Employee.CompanyId);
                if (!allowedCompanyIds.Contains(jobPosition.CompanyId))
                    return BadRequest(new { Message = "Le poste de travail ne correspond pas à la société spécifiée" });
            }

            // Vérifier que le ContractType existe et appartient à la bonne société (si fourni)
            if (dto.ContractTypeId.HasValue)
            {
                var contractType = await _db.ContractTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ct => ct.Id == dto.ContractTypeId && ct.DeletedAt == null);

                if (contractType == null)
                    return NotFound(new { Message = "Type de contrat non trouvé" });

                if (contractType.CompanyId != companyId)
                    return BadRequest(new { Message = "Le type de contrat ne correspond pas à la société spécifiée" });
            }

            // Vérifier que le CIN n'existe pas déjà
            if (await _db.Employees.AnyAsync(e => e.CinNumber == dto.CinNumber && e.DeletedAt == null))
                return Conflict(new { Message = "Un employé avec ce numéro CIN existe déjà" });

            // Vérifier que l'email n'existe pas déjà (dans Employees)
            if (await _db.Employees.AnyAsync(e => e.Email == dto.Email && e.DeletedAt == null))
                return Conflict(new { Message = "Un employé avec cet email existe déjà" });

            // Vérifier que l'email n'existe pas déjà (dans Users)
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.DeletedAt == null))
                return Conflict(new { Message = "Un utilisateur avec cet email existe déjà" });

            // Vérifier que le manager existe (si fourni)
            if (dto.ManagerId.HasValue)
            {
                var managerExists = await _db.Employees.AnyAsync(e => e.Id == dto.ManagerId && e.DeletedAt == null);
                if (!managerExists)
                    return NotFound(new { Message = "Manager non trouvé" });
            }

            // Vérifier que la catégorie existe et appartient à la même company
            var category = await _db.EmployeeCategories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.DeletedAt == null);
                
            if (category == null)
                return NotFound(new { Message = "Catégorie d'employe non trouvée" });

            if (category.CompanyId != companyId)
                return BadRequest(new { Message = "La catégorie ne correspond à la société spécifiée" });

            // ===== GÉNÉRATION DU MATRICULE UNIQUE =====
            int? newMatricule = await GenerateUniqueMatricule(companyId);

            // Créer l'employé avec le CompanyId déterminé et le Matricule généré
            var employee = new payzen_backend.Models.Employee.Employee
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CinNumber = dto.CinNumber,
                DateOfBirth = dto.DateOfBirth,
                Phone = dto.Phone,
                Email = dto.Email,
                CompanyId = companyId,
                DepartementId = dto.DepartementId,
                ManagerId = dto.ManagerId,
                StatusId = dto.StatusId,
                GenderId = dto.GenderId,
                NationalityId = dto.NationalityId,
                EducationLevelId = dto.EducationLevelId,
                MaritalStatusId = dto.MaritalStatusId,
                CnssNumber = dto.CnssNumber,
                CimrNumber = dto.CimrNumber,
                Matricule = newMatricule,
                CreatedAt = DateTimeOffset.UtcNow,
                CategoryId = dto.CategoryId,
                CreatedBy = userId
            };

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();

            // Log creation d'employé
            await _eventLogService.LogSimpleEventAsync(
                employee.Id,
                EmployeeEventLogService.EventNames.EmployeeCreated,
                null,
                $"{employee.FirstName} {employee.LastName} (Matricule: {employee.Matricule})",
                userId);

            // ===== CRÉER LE CONTRAT DE L'EMPLOYÉ (si JobPosition et ContractType sont fournis) =====
            if (dto.JobPositionId.HasValue && dto.ContractTypeId.HasValue)
            {
                // récupérer noms pour le log
                var jpName = await _db.JobPositions
                    .AsNoTracking()
                    .Where(jp => jp.Id == dto.JobPositionId.Value)
                    .Select(jp => jp.Name)
                    .FirstOrDefaultAsync();

                var ctName = await _db.ContractTypes
                    .AsNoTracking()
                    .Where(ct => ct.Id == dto.ContractTypeId.Value)
                    .Select(ct => ct.ContractTypeName)
                    .FirstOrDefaultAsync();

                var employeeContract = new EmployeeContract
                {
                    EmployeeId = employee.Id,
                    CompanyId = companyId,
                    JobPositionId = dto.JobPositionId.Value,
                    ContractTypeId = dto.ContractTypeId.Value,
                    StartDate = dto.StartDate ?? DateTime.UtcNow,
                    EndDate = null, // Contrat actif
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                };

                _db.EmployeeContracts.Add(employeeContract);
                await _db.SaveChangesAsync();

                // Log création du contrat
                await _eventLogService.LogSimpleEventAsync(
                    employee.Id,
                    EmployeeEventLogService.EventNames.ContractCreated,
                    null,
                    $"{jpName ?? "N/A"} - {ctName ?? "N/A"}",
                    userId);
            }

            // ===== CRÉER LE SALAIRE DE L'EMPLOYÉ (si fourni) =====
            if (dto.Salary.HasValue && dto.Salary.Value > 0)
            {
                // récupérer contrat actif si besoin pour info (optionnel)
                var activeContract = await _db.EmployeeContracts
                    .AsNoTracking()
                    .Where(c => c.EmployeeId == employee.Id && c.DeletedAt == null && c.EndDate == null)
                    .OrderByDescending(c => c.StartDate)
                    .FirstOrDefaultAsync();

                var employeeSalary = new EmployeeSalary
                {
                    EmployeeId = employee.Id,
                    ContractId = activeContract?.Id ?? 0,
                    BaseSalary = dto.Salary.Value,
                    EffectiveDate = dto.StartDate ?? DateTime.UtcNow,
                    EndDate = null, // Salaire actif
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                };

                _db.EmployeeSalaries.Add(employeeSalary);
                await _db.SaveChangesAsync();

                // Log création du salaire
                await _eventLogService.LogSimpleEventAsync(
                    employee.Id,
                    EmployeeEventLogService.EventNames.SalaryCreated,
                    null,
                    employeeSalary.BaseSalary.ToString("N2"),
                    userId);
            }

            // ===== CRÉER L'ADRESSE DE L'EMPLOYÉ (si fournie) =====
            if (dto.CityId.HasValue && !string.IsNullOrEmpty(dto.AddressLine1))
            {
                var employeeAddress = new EmployeeAddress
                {
                    EmployeeId = employee.Id,
                    CityId = dto.CityId.Value,
                    AddressLine1 = dto.AddressLine1,
                    AddressLine2 = dto.AddressLine2,
                    ZipCode = dto.ZipCode,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                };

                _db.EmployeeAddresses.Add(employeeAddress);
                await _db.SaveChangesAsync();

                // Log création d'adresse
                var cityName = await _db.Cities.AsNoTracking()
                    .Where(c => c.Id == dto.CityId.Value)
                    .Select(c => c.CityName)
                    .FirstOrDefaultAsync();

                await _eventLogService.LogSimpleEventAsync(
                    employee.Id,
                    EmployeeEventLogService.EventNames.AddressCreated,
                    null,
                    $"{employeeAddress.AddressLine1}{(string.IsNullOrEmpty(employeeAddress.AddressLine2) ? "" : $" - {employeeAddress.AddressLine2}")}, {cityName ?? "N/A"}",
                    userId);
            }

            // Créer automatiquement un compte utilisateur si demandé
            string? temporaryPassword = null;
            Users? createdUser = null;

            if (dto.CreateUserAccount)
            {
                // Générer un nom d'utilisateur unique
                var baseUsername = _passwordGenerator.GenerateUsername(dto.FirstName, dto.LastName);
                var username = baseUsername;
                var suffix = 1;

                while (await _db.Users.AnyAsync(u => u.Username == username && u.DeletedAt == null))
                {
                    username = _passwordGenerator.GenerateUsername(dto.FirstName, dto.LastName, suffix);
                    suffix++;
                }

                // Utiliser le mot de passe fourni ou générer un temporaire
                temporaryPassword = dto.Password ?? _passwordGenerator.GenerateTemporaryPassword();

                // Créer le compte utilisateur
                createdUser = new Users
                {
                    EmployeeId = employee.Id,
                    Username = username,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                };

                _db.Users.Add(createdUser);
                await _db.SaveChangesAsync();

                Console.WriteLine($"User créé automatiquement - Username: {username}, Email: {dto.Email}");

                // Log création du compte utilisateur lié à l'employé
                await _eventLogService.LogSimpleEventAsync(
                    employee.Id,
                    "UserAccount_Created",
                    null,
                    username,
                    userId);
            }

            // Récupérer l'employé créé avec ses relations
            var createdEmployee = await _db.Employees
                .AsNoTracking()
                .Include(e => e.Company)
                .Include(e => e.Manager)
                .Include(e => e.Departement)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.JobPosition)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.ContractType)
                .FirstAsync(e => e.Id == employee.Id);

            // Récupérer le contrat actif pour le DTO
            var activeContractFinal = createdEmployee.Contracts?
                .Where(c => c.DeletedAt == null && c.EndDate == null)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefault();

            var readDto = new EmployeeReadDto
            {
                Id = createdEmployee.Id,
                Matricule = createdEmployee.Matricule,
                FirstName = createdEmployee.FirstName,
                LastName = createdEmployee.LastName,
                CinNumber = createdEmployee.CinNumber,
                DateOfBirth = createdEmployee.DateOfBirth,
                Phone = createdEmployee.Phone,
                Email = createdEmployee.Email,
                CompanyId = createdEmployee.CompanyId,
                CompanyName = createdEmployee.Company?.CompanyName ?? "",
                DepartementId = createdEmployee.DepartementId,
                DepartementName = createdEmployee.Departement?.DepartementName,
                ManagerId = createdEmployee.ManagerId,
                ManagerName = createdEmployee.Manager != null ? $"{createdEmployee.Manager.FirstName} {createdEmployee.Manager.LastName}" : null,
                StatusId = createdEmployee.StatusId,
                GenderId = createdEmployee.GenderId,
                NationalityId = createdEmployee.NationalityId,
                EducationLevelId = createdEmployee.EducationLevelId,
                MaritalStatusId = createdEmployee.MaritalStatusId,
                JobPostionName = activeContractFinal?.JobPosition?.Name,
                CreatedAt = createdEmployee.CreatedAt.DateTime
            };

            // Assigné un role par défaut au nouvel utilisateur
            if (dto.CreateUserAccount && createdUser != null)
            {
                var defaultRole = await _db.Roles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Name == "employee" && r.DeletedAt == null);
                if (defaultRole != null)
                {
                    var userRole = new UsersRoles
                    {
                        UserId = createdUser.Id,
                        RoleId = defaultRole.Id,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = userId
                    };
                    _db.UsersRoles.Add(userRole);
                    await _db.SaveChangesAsync();
                }
            }

            // Retourner les infos du compte créé dans la réponse
            if (dto.CreateUserAccount && createdUser != null)
            {
                return CreatedAtAction(nameof(GetById), new { id = employee.Id }, new
                {
                    Employee = readDto,
                    UserAccount = new
                    {
                        createdUser.Username,
                        createdUser.Email,
                        TemporaryPassword = temporaryPassword,
                        Message = "Un compte utilisateur a été créé. Le mot de passe temporaire doit être changé lors de la première connexion."
                    }
                });
            }

            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, readDto);
        }
        /// <summary>
        /// Met à jour un employé
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission(EDIT_EMPLOYEE)]
        public async Task<ActionResult<EmployeeReadDto>> Update(int id, [FromBody] EmployeeUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // Récupérer l'utilisateur authentifié
            var currentUser = await _db.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.DeletedAt == null);

            if (currentUser?.Employee == null)
                return BadRequest(new { Message = "L'utilisateur n'est pas associé à un employé" });

            // Récupérer l'employé avec toutes les relations nécessaires pour le logging
            var employee = await _db.Employees
                .Include(e => e.Status)
                .Include(e => e.Gender)
                .Include(e => e.Nationality)
                .Include(e => e.EducationLevel)
                .Include(e => e.MaritalStatus)
                .Include(e => e.Departement)
                .Include(e => e.Manager)
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);

            if (employee == null)
                return NotFound(new { Message = "Employé non trouvé" });

            // Vérifier les permissions
            if (employee.CompanyId != currentUser.Employee.CompanyId)
            {
                // TODO: Vérifier si l'utilisateur a les permissions pour modifier des employés d'autres sociétés
                return Forbid();
            }

            var updateTime = DateTimeOffset.UtcNow;
            bool hasChanges = false;

            // ===== MISE À JOUR AVEC LOGGING =====

            // FirstName
            if (dto.FirstName != null && dto.FirstName != employee.FirstName)
            {
                await _eventLogService.LogSimpleEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.FirstNameChanged,
                    oldValue: employee.FirstName,
                    newValue: dto.FirstName,
                    createdBy: userId
                );
                employee.FirstName = dto.FirstName;
                hasChanges = true;
            }

            // LastName
            if (dto.LastName != null && dto.LastName != employee.LastName)
            {
                await _eventLogService.LogSimpleEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.LastNameChanged,
                    oldValue: employee.LastName,
                    newValue: dto.LastName,
                    createdBy: userId
                );
                employee.LastName = dto.LastName;
                hasChanges = true;
            }

            // CIN Number
            if (dto.CinNumber != null && dto.CinNumber != employee.CinNumber)
            {
                if (await _db.Employees.AnyAsync(e => e.CinNumber == dto.CinNumber && e.Id != id && e.DeletedAt == null))
                    return Conflict(new { Message = "Un employé avec ce numéro CIN existe déjà" });

                await _eventLogService.LogSimpleEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.CinNumberChanged,
                    oldValue: employee.CinNumber,
                    newValue: dto.CinNumber,
                    createdBy: userId
                );
                employee.CinNumber = dto.CinNumber;
                hasChanges = true;
            }

            // Date of Birth
            if (dto.DateOfBirth.HasValue && dto.DateOfBirth != employee.DateOfBirth)
            {
                await _eventLogService.LogSimpleEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.DateOfBirthChanged,
                    oldValue: employee.DateOfBirth.ToString("yyyy-MM-dd"),
                    newValue: dto.DateOfBirth.Value.ToString("yyyy-MM-dd"),
                    createdBy: userId
                );
                employee.DateOfBirth = dto.DateOfBirth.Value;
                hasChanges = true;
            }

            // Phone
            if (!string.IsNullOrEmpty(dto.Phone) && dto.Phone != employee.Phone)
            {
                await _eventLogService.LogSimpleEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.PhoneChanged,
                    oldValue: employee.Phone,
                    newValue: dto.Phone,
                    createdBy: userId
                );
                employee.Phone = dto.Phone;
                hasChanges = true;
            }

            // Email
            if (dto.Email != null && dto.Email != employee.Email)
            {
                if (await _db.Employees.AnyAsync(e => e.Email == dto.Email && e.Id != id && e.DeletedAt == null))
                    return Conflict(new { Message = "Un employé avec cet email existe déjà" });

                if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.EmployeeId != id && u.DeletedAt == null))
                    return Conflict(new { Message = "Un utilisateur avec cet email existe déjà" });

                await _eventLogService.LogSimpleEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.EmailChanged,
                    oldValue: employee.Email,
                    newValue: dto.Email,
                    createdBy: userId
                );
                employee.Email = dto.Email;
                hasChanges = true;
            }

            // Department
            if (dto.DepartementId.HasValue && dto.DepartementId != employee.DepartementId)
            {
                var newDepartement = await _db.Departement
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == dto.DepartementId && d.DeletedAt == null);

                if (newDepartement == null)
                    return NotFound(new { Message = "Département non trouvé" });

                if (newDepartement.CompanyId != employee.CompanyId)
                    return BadRequest(new { Message = "Le département ne correspond pas à la société de l'employé" });

                await _eventLogService.LogRelationEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.DepartmentChanged,
                    oldValueId: employee.DepartementId,
                    oldValueName: employee.Departement?.DepartementName,
                    newValueId: dto.DepartementId,
                    newValueName: newDepartement.DepartementName,
                    createdBy: userId
                );

                employee.DepartementId = dto.DepartementId;
                hasChanges = true;
            }

            // Manager
            if (dto.ManagerId.HasValue && dto.ManagerId != employee.ManagerId)
            {
                if (dto.ManagerId.Value == id)
                    return BadRequest(new { Message = "Un employé ne peut pas être son propre manager" });

                var newManager = await _db.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == dto.ManagerId && e.DeletedAt == null);

                if (newManager == null)
                    return NotFound(new { Message = "Manager non trouvé" });

                await _eventLogService.LogRelationEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.ManagerChanged,
                    oldValueId: employee.ManagerId,
                    oldValueName: employee.Manager != null ? $"{employee.Manager.FirstName} {employee.Manager.LastName}" : null,
                    newValueId: dto.ManagerId,
                    newValueName: $"{newManager.FirstName} {newManager.LastName}",
                    createdBy: userId
                );

                employee.ManagerId = dto.ManagerId;
                hasChanges = true;
            }

            // Status
            if (dto.StatusId.HasValue && dto.StatusId != employee.StatusId)
            {
                var newStatus = await _db.Statuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == dto.StatusId);

                if (newStatus == null)
                    return NotFound(new { Message = "Statut non trouvé" });

                await _eventLogService.LogRelationEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.StatusChanged,
                    oldValueId: employee.StatusId,
                    oldValueName: employee.Status?.Code,
                    newValueId: dto.StatusId,
                    newValueName: newStatus.Code,
                    createdBy: userId
                );

                employee.StatusId = dto.StatusId;
                hasChanges = true;
            }

            // Gender
            if (dto.GenderId.HasValue && dto.GenderId != employee.GenderId)
            {
                var newGender = await _db.Genders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == dto.GenderId);

                if (newGender == null)
                    return NotFound(new { Message = "Genre non trouvé" });

                await _eventLogService.LogRelationEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.GenderChanged,
                    oldValueId: employee.GenderId,
                    oldValueName: employee.Gender?.Code,
                    newValueId: dto.GenderId,
                    newValueName: newGender.Code,
                    createdBy: userId
                );

                employee.GenderId = dto.GenderId;
                hasChanges = true;
            }

            // Nationality
            if (dto.NationalityId.HasValue && dto.NationalityId != employee.NationalityId)
            {
                var newNationality = await _db.Nationalities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == dto.NationalityId && n.DeletedAt == null);

                if (newNationality == null)
                    return NotFound(new { Message = "Nationalité non trouvée" });

                await _eventLogService.LogRelationEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.NationalityChanged,
                    oldValueId: employee.NationalityId,
                    oldValueName: employee.Nationality?.Name,
                    newValueId: dto.NationalityId,
                    newValueName: newNationality.Name,
                    createdBy: userId
                );

                employee.NationalityId = dto.NationalityId;
                hasChanges = true;
            }

            // Education Level
            if (dto.EducationLevelId.HasValue && dto.EducationLevelId != employee.EducationLevelId)
            {
                var newEducationLevel = await _db.EducationLevels
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == dto.EducationLevelId);

                if (newEducationLevel == null)
                    return NotFound(new { Message = "Niveau d'éducation non trouvé" });

                await _eventLogService.LogRelationEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.EducationLevelChanged,
                    oldValueId: employee.EducationLevelId,
                    oldValueName: employee.EducationLevel?.Code,
                    newValueId: dto.EducationLevelId,
                    newValueName: newEducationLevel.Code,
                    createdBy: userId
                );

                employee.EducationLevelId = dto.EducationLevelId;
                hasChanges = true;
            }

            // Marital Status
            if (dto.MaritalStatusId.HasValue && dto.MaritalStatusId != employee.MaritalStatusId)
            {
                var newMaritalStatus = await _db.MaritalStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == dto.MaritalStatusId);

                if (newMaritalStatus == null)
                    return NotFound(new { Message = "Statut matrimonial non trouvé" });

                await _eventLogService.LogRelationEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.MaritalStatusChanged,
                    oldValueId: employee.MaritalStatusId,
                    oldValueName: employee.MaritalStatus?.Code,
                    newValueId: dto.MaritalStatusId,
                    newValueName: newMaritalStatus.Code,
                    createdBy: userId
                );

                employee.MaritalStatusId = dto.MaritalStatusId;
                hasChanges = true;
            }

            // CNSS Number
            if (dto.CnssNumber.HasValue && dto.CnssNumber.ToString() != employee.CnssNumber)
            {
                await _eventLogService.LogSimpleEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.CnssNumberChanged,
                    oldValue: employee.CnssNumber,
                    newValue: dto.CnssNumber.ToString(),
                    createdBy: userId
                );
                employee.CnssNumber = dto.CnssNumber.ToString();
                hasChanges = true;
            }

            // CIMR Number
            if (dto.CimrNumber.HasValue && dto.CimrNumber.ToString() != employee.CimrNumber)
            {
                await _eventLogService.LogSimpleEventAsync(
                    employeeId: id,
                    eventName: EmployeeEventLogService.EventNames.CimrNumberChanged,
                    oldValue: employee.CimrNumber,
                    newValue: dto.CimrNumber.ToString(),
                    createdBy: userId
                );
                employee.CimrNumber = dto.CimrNumber.ToString();
                hasChanges = true;
            }

            // ===== MISE À JOUR DU CONTRAT (avec historique) =====
            if ((dto.JobPositionId.HasValue || dto.ContractTypeId.HasValue) && dto.ContractStartDate.HasValue)
            {
                // Récupérer le contrat actif
                var activeContractForUpdate = await _db.EmployeeContracts
                    .Include(c => c.JobPosition)
                    .Include(c => c.ContractType)
                    .FirstOrDefaultAsync(c =>
                        c.EmployeeId == id &&
                        c.DeletedAt == null &&
                        c.EndDate == null);

                bool contractChanged = false;
                string? oldJobPositionName = null;
                string? newJobPositionName = null;
                string? oldContractTypeName = null;
                string? newContractTypeName = null;

                // Vérifier si le JobPosition a changé
                if (dto.JobPositionId.HasValue && activeContractForUpdate?.JobPositionId != dto.JobPositionId)
                {
                    var newJobPosition = await _db.JobPositions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(jp => jp.Id == dto.JobPositionId && jp.DeletedAt == null);

                    if (newJobPosition == null)
                        return NotFound(new { Message = "Poste de travail non trouvé" });

                    if (newJobPosition.CompanyId != employee.CompanyId)
                        return BadRequest(new { Message = "Le poste de travail ne correspond pas à la société de l'employé" });

                    oldJobPositionName = activeContractForUpdate?.JobPosition?.Name;
                    newJobPositionName = newJobPosition.Name;
                    contractChanged = true;
                }

                // Vérifier si le ContractType a changé
                if (dto.ContractTypeId.HasValue && activeContractForUpdate?.ContractTypeId != dto.ContractTypeId)
                {
                    var newContractType = await _db.ContractTypes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(ct => ct.Id == dto.ContractTypeId && ct.DeletedAt == null);

                    if (newContractType == null)
                        return NotFound(new { Message = "Type de contrat non trouvé" });

                    if (newContractType.CompanyId != employee.CompanyId)
                        return BadRequest(new { Message = "Le type de contrat ne correspond pas à la société de l'employé" });

                    oldContractTypeName = activeContractForUpdate?.ContractType?.ContractTypeName;
                    newContractTypeName = newContractType.ContractTypeName;
                    contractChanged = true;
                }

                // Si des changements sont détectés, créer un nouveau contrat et fermer l'ancien
                if (contractChanged)
                {
                    if (activeContractForUpdate != null)
                    {
                        // Fermer l'ancien contrat
                        activeContractForUpdate.EndDate = dto.ContractStartDate.Value;
                        activeContractForUpdate.ModifiedAt = updateTime;
                        activeContractForUpdate.ModifiedBy = userId;

                        // Logger la fin de contrat
                        await _eventLogService.LogSimpleEventAsync(
                            employeeId: id,
                            eventName: EmployeeEventLogService.EventNames.ContractTerminated,
                            oldValue: $"{oldJobPositionName} - {oldContractTypeName}",
                            newValue: dto.ContractStartDate.Value.ToString("yyyy-MM-dd"),
                            createdBy: userId
                        );
                    }

                    // Créer le nouveau contrat
                    var newContract = new EmployeeContract
                    {
                        EmployeeId = id,
                        CompanyId = employee.CompanyId,
                        JobPositionId = dto.JobPositionId ?? activeContractForUpdate!.JobPositionId,
                        ContractTypeId = dto.ContractTypeId ?? activeContractForUpdate!.ContractTypeId,
                        StartDate = dto.ContractStartDate.Value,
                        EndDate = null,
                        CreatedAt = updateTime,
                        CreatedBy = userId
                    };

                    _db.EmployeeContracts.Add(newContract);
                    await _db.SaveChangesAsync();

                    // Logger les changements
                    if (dto.JobPositionId.HasValue && oldJobPositionName != newJobPositionName)
                    {
                        await _eventLogService.LogRelationEventAsync(
                            employeeId: id,
                            eventName: EmployeeEventLogService.EventNames.JobPositionChanged,
                            oldValueId: activeContractForUpdate?.JobPositionId,
                            oldValueName: oldJobPositionName,
                            newValueId: dto.JobPositionId,
                            newValueName: newJobPositionName,
                            createdBy: userId
                        );
                    }

                    if (dto.ContractTypeId.HasValue && oldContractTypeName != newContractTypeName)
                    {
                        await _eventLogService.LogRelationEventAsync(
                            employeeId: id,
                            eventName: EmployeeEventLogService.EventNames.ContractTypeChanged,
                            oldValueId: activeContractForUpdate?.ContractTypeId,
                            oldValueName: oldContractTypeName,
                            newValueId: dto.ContractTypeId,
                            newValueName: newContractTypeName,
                            createdBy: userId
                        );
                    }

                    // Logger la création du nouveau contrat
                    await _eventLogService.LogSimpleEventAsync(
                        employeeId: id,
                        eventName: EmployeeEventLogService.EventNames.ContractCreated,
                        oldValue: null,
                        newValue: $"{newJobPositionName ?? oldJobPositionName} - {newContractTypeName ?? oldContractTypeName}",
                        createdBy: userId
                    );

                    hasChanges = true;
                }
            }

            // ===== MISE À JOUR DU SALAIRE (avec historique) =====
            if (dto.Salary.HasValue && dto.SalaryEffectiveDate.HasValue)
            {
                // Récupérer le contrat actif (nécessaire pour le salaire)
                var activeContractForSalary = await _db.EmployeeContracts  // ← RENOMMÉ
                    .FirstOrDefaultAsync(c =>
                        c.EmployeeId == id &&
                        c.DeletedAt == null &&
                        c.EndDate == null);

                if (activeContractForSalary == null)  // ← RENOMMÉ
                    return BadRequest(new { Message = "Aucun contrat actif trouvé pour cet employé. Veuillez d'abord créer un contrat." });

                // Récupérer le salaire actif
                var activeSalary = await _db.EmployeeSalaries
                    .FirstOrDefaultAsync(s =>
                        s.EmployeeId == id &&
                        s.DeletedAt == null &&
                        s.EndDate == null);

                // Vérifier si le salaire a changé
                if (activeSalary == null || activeSalary.BaseSalary != dto.Salary.Value)
                {
                    decimal oldSalary = activeSalary?.BaseSalary ?? 0;

                    if (activeSalary != null)
                    {
                        // Fermer l'ancien salaire
                        activeSalary.EndDate = dto.SalaryEffectiveDate.Value;
                        activeSalary.ModifiedAt = updateTime;
                        activeSalary.ModifiedBy = userId;
                    }

                    // Créer le nouveau salaire
                    var newSalary = new EmployeeSalary
                    {
                        EmployeeId = id,
                        ContractId = activeContractForSalary.Id,
                        BaseSalary = dto.Salary.Value,
                        EffectiveDate = dto.SalaryEffectiveDate.Value,
                        EndDate = null,
                        CreatedAt = updateTime,
                        CreatedBy = userId
                    };

                    _db.EmployeeSalaries.Add(newSalary);
                    await _db.SaveChangesAsync();

                    // Logger le changement de salaire
                    await _eventLogService.LogSimpleEventAsync(
                        employeeId: id,
                        eventName: EmployeeEventLogService.EventNames.SalaryUpdated,
                        oldValue: oldSalary.ToString("N2"),
                        newValue: dto.Salary.Value.ToString("N2"),
                        createdBy: userId
                    );

                    hasChanges = true;
                }
            }

            // ===== MISE À JOUR DE L'ADRESSE =====
            if (dto.CityId.HasValue && !string.IsNullOrEmpty(dto.AddressLine1) && !string.IsNullOrEmpty(dto.ZipCode))
            {
                // Récupérer l'adresse active
                var activeAddress = await _db.EmployeeAddresses
                    .Include(a => a.City)
                    .FirstOrDefaultAsync(a =>
                        a.EmployeeId == id &&
                        a.DeletedAt == null);

                bool addressChanged = false;

                // Vérifier si l'adresse a changé
                if (activeAddress != null)
                {
                    if (activeAddress.CityId != dto.CityId ||
                        activeAddress.AddressLine1 != dto.AddressLine1 ||
                        activeAddress.AddressLine2 != dto.AddressLine2 ||
                        activeAddress.ZipCode != dto.ZipCode)
                    {
                        addressChanged = true;
                    }
                }
                else
                {
                    addressChanged = true; // Nouvelle adresse
                }

                if (addressChanged)
                {
                    var newCity = await _db.Cities
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == dto.CityId && c.DeletedAt == null);

                    if (newCity == null)
                    {
                        Console.WriteLine("  ❌ Ville non trouvée");
                        return NotFound(new { Message = "Ville non trouvée" });
                    }

                    string? oldAddressValue = null;
                    if (activeAddress != null)
                    {
                        // Marquer l'ancienne adresse comme supprimée (soft delete)
                        activeAddress.DeletedAt = updateTime;
                        activeAddress.DeletedBy = userId;

                        oldAddressValue = $"{activeAddress.AddressLine1}, {activeAddress.City?.CityName}";
                    }

                    // Créer la nouvelle adresse
                    var newAddress = new EmployeeAddress
                    {
                        EmployeeId = id,
                        CityId = dto.CityId.Value,
                        AddressLine1 = dto.AddressLine1,
                        AddressLine2 = dto.AddressLine2,
                        ZipCode = dto.ZipCode,
                        CreatedAt = updateTime,
                        CreatedBy = userId
                    };

                    _db.EmployeeAddresses.Add(newAddress);
                    await _db.SaveChangesAsync();

                    // Logger le changement d'adresse
                    await _eventLogService.LogSimpleEventAsync(
                        employeeId: id,
                        eventName: activeAddress == null
                            ? EmployeeEventLogService.EventNames.AddressCreated
                            : EmployeeEventLogService.EventNames.AddressUpdated,
                        oldValue: oldAddressValue,
                        newValue: $"{dto.AddressLine1}, {newCity.CityName}",
                        createdBy: userId
                    );

                    hasChanges = true;
                    Console.WriteLine("  ✓ Adresse modifiée avec succès");
                }
                else
                {
                    Console.WriteLine("  ⊘ Aucune modification d'adresse");
                }
            }
            else if (dto.CityId.HasValue || !string.IsNullOrEmpty(dto.AddressLine1) || !string.IsNullOrEmpty(dto.ZipCode))
            {
                // Cas où on essaie de supprimer une adresse en laissant des champs vides
                return BadRequest(new { Message = "Impossible de supprimer l'adresse en laissant des champs vides" });
            }

            // Mettre à jour ModifiedAt et ModifiedBy si des changements ont été effectués
            if (hasChanges)
            {
                employee.ModifiedAt = updateTime;
                employee.ModifiedBy = userId;
                await _db.SaveChangesAsync();
            }

            // Récupérer l'employé mis à jour avec ses relations
            var updatedEmployee = await _db.Employees
                .AsNoTracking()
                .Include(e => e.Company)
                .Include(e => e.Manager)
                .Include(e => e.Departement)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.JobPosition)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.ContractType)
                .FirstAsync(e => e.Id == id);

            var activeContract = updatedEmployee.Contracts?
                .Where(c => c.DeletedAt == null && c.EndDate == null)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefault();

            var readDto = new EmployeeReadDto
            {
                Id = updatedEmployee.Id,
                FirstName = updatedEmployee.FirstName,
                LastName = updatedEmployee.LastName,
                CinNumber = updatedEmployee.CinNumber,
                DateOfBirth = updatedEmployee.DateOfBirth,
                Phone = updatedEmployee.Phone,
                Email = updatedEmployee.Email,
                CompanyId = updatedEmployee.CompanyId,
                CompanyName = updatedEmployee.Company?.CompanyName ?? "",
                DepartementId = updatedEmployee.DepartementId,
                DepartementName = updatedEmployee.Departement?.DepartementName,
                ManagerId = updatedEmployee.ManagerId,
                ManagerName = updatedEmployee.Manager != null
                    ? $"{updatedEmployee.Manager.FirstName} {updatedEmployee.Manager.LastName}"
                    : null,
                StatusId = updatedEmployee.StatusId,
                GenderId = updatedEmployee.GenderId,
                EducationLevelId = updatedEmployee.EducationLevelId,
                MaritalStatusId = updatedEmployee.MaritalStatusId,
                JobPostionName = activeContract?.JobPosition?.Name,
                CreatedAt = updatedEmployee.CreatedAt.DateTime
            };

            return Ok(readDto);
        }

        /// <summary>
        /// Récupère toutes les données nécessaires pour le formulaire de création/modification d'employé
        /// </summary>
        /// <param name="companyId">ID de l'entreprise (optionnel, si non fourni utilise l'entreprise de l'utilisateur connecté)</param>
        [HttpGet("form-data")]
        public async Task<ActionResult<EmployeeFormDataDto>> GetFormData([FromQuery] int? companyId = null)
        {
            // Récupérer l'utilisateur authentifié
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Message = "Utilisateur non authentifié" });
            }

            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.DeletedAt == null);

            if (user?.Employee == null)
            {
                return BadRequest(new { Message = "L'utilisateur n'est pas associé à un employé" });
            }

            // Utiliser le companyId fourni ou celui de l'utilisateur
            var targetCompanyId = companyId ?? user.Employee.CompanyId;

            // Vérifier que l'utilisateur a accès à cette entreprise
            if (companyId.HasValue && companyId.Value != user.Employee.CompanyId)
            {
                var managerCompany = await _db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == user.Employee.CompanyId && c.DeletedAt == null);

                var targetCompany = await _db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == companyId.Value && c.DeletedAt == null);

                if (targetCompany == null)
                    return NotFound(new { Message = "Société cible non trouvée" });

                var allowedForExpert = managerCompany != null
                                       && managerCompany.IsCabinetExpert
                                       && targetCompany.ManagedByCompanyId == managerCompany.Id;

                if (!allowedForExpert)
                    return Forbid();
            }

            var formData = new EmployeeFormDataDto();

            // 1. Récupérer les statuts (Read DTO)
            formData.Statuses = await _db.Statuses
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Code)
                .Select(s => new Models.Referentiel.Dtos.StatusReadDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    NameFr = s.NameFr,
                    NameAr = s.NameAr,
                    NameEn = s.NameEn,
                    IsActive = s.IsActive,
                    AffectsAccess = s.AffectsAccess,
                    AffectsPayroll = s.AffectsPayroll,
                    AffectsAttendance = s.AffectsAttendance,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            // 2. Récupérer les genres (Read DTO)
            formData.Genders = await _db.Genders
                .AsNoTracking()
                .Where(g => g.IsActive)
                .OrderBy(g => g.Code)
                .Select(g => new Models.Referentiel.Dtos.GenderReadDto
                {
                    Id = g.Id,
                    Code = g.Code,
                    NameFr = g.NameFr,
                    NameAr = g.NameAr,
                    NameEn = g.NameEn,
                    IsActive = g.IsActive,
                    CreatedAt = g.CreatedAt
                })
                .ToListAsync();

            // 3. Récupérer les niveaux d'éducation (Read DTO)
            formData.EducationLevels = await _db.EducationLevels
                .AsNoTracking()
                .Where(e => e.IsActive)
                .OrderBy(e => e.LevelOrder)
                .ThenBy(e => e.NameFr)
                .Select(e => new Models.Referentiel.Dtos.EducationLevelReadDto
                {
                    Id = e.Id,
                    Code = e.Code,
                    NameFr = e.NameFr,
                    NameAr = e.NameAr,
                    NameEn = e.NameEn,
                    LevelOrder = e.LevelOrder,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            // 4. Récupérer les statuts matrimoniaux (Read DTO)
            formData.MaritalStatuses = await _db.MaritalStatuses
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.NameFr)
                .Select(m => new Models.Referentiel.Dtos.MaritalStatusReadDto
                {
                    Id = m.Id,
                    Code = m.Code,
                    NameFr = m.NameFr,
                    NameAr = m.NameAr,
                    NameEn = m.NameEn,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            // 5. Récupérer les pays
            formData.Countries = await _db.Countries
                .Where(c => c.DeletedAt == null)
                .Select(c => new CountryDto
                {
                    Id = c.Id,
                    CountryName = c.CountryName,
                    CountryPhoneCode = c.CountryPhoneCode
                })
                .OrderBy(c => c.CountryName)
                .ToListAsync();

            // 6. Récupérer les villes
            formData.Cities = await _db.Cities
                .Where(c => c.DeletedAt == null)
                .Include(c => c.Country)
                .Select(c => new CityDto
                {
                    Id = c.Id,
                    CityName = c.CityName,
                    CountryId = c.CountryId,
                    CountryName = c.Country != null ? c.Country.CountryName : null
                })
                .OrderBy(c => c.CountryName)
                .ThenBy(c => c.CityName)
                .ToListAsync();

            // 7. Récupérer les départements de l'entreprise
            formData.Departements = await _db.Departement
                .Where(d => d.DeletedAt == null && d.CompanyId == targetCompanyId)
                .Select(d => new DepartementDto
                {
                    Id = d.Id,
                    DepartementName = d.DepartementName,
                    CompanyId = d.CompanyId
                })
                .OrderBy(d => d.DepartementName)
                .ToListAsync();

            // 8. Récupérer les postes de l'entreprise
            formData.JobPositions = await _db.JobPositions
                .Where(j => j.DeletedAt == null && j.CompanyId == targetCompanyId)
                .Select(j => new JobPositionDto
                {
                    Id = j.Id,
                    Name = j.Name,
                    CompanyId = j.CompanyId
                })
                .OrderBy(j => j.Name)
                .ToListAsync();

            // 9. Récupérer les types de contrat de l'entreprise
            formData.ContractTypes = await _db.ContractTypes
                .Where(c => c.DeletedAt == null && c.CompanyId == targetCompanyId)
                .Select(c => new ContractTypeDto
                {
                    Id = c.Id,
                    ContractTypeName = c.ContractTypeName,
                    CompanyId = c.CompanyId
                })
                .OrderBy(c => c.ContractTypeName)
                .ToListAsync();

            // 10. Récupérer les managers potentiels (employés actifs de l'entreprise)
            formData.PotentialManagers = await _db.Employees
                .Where(e => e.DeletedAt == null && e.CompanyId == targetCompanyId)
                .Include(e => e.Departement)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    FullName = e.FirstName + " " + e.LastName,
                    DepartementName = e.Departement != null ? e.Departement.DepartementName : null
                })
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();

            // 11. Récupérer les nationalités
            formData.Nationalities = await _db.Nationalities
                .Where(n => n.DeletedAt == null)
                .Select(n => new NationalityDto
                {
                    Id = n.Id,
                    Name = n.Name
                })
                .OrderBy(n => n.Name)
                .ToListAsync();

            // 12. Récupérer les catégories d'employés de l'entreprise
            formData.EmployeeCategories = await _db.EmployeeCategories
                .Where(c => c.DeletedAt == null && c.CompanyId == targetCompanyId)
                .OrderBy(c => c.Name)
                .Select(c => new Models.Employee.Dtos.EmployeeCategorySimpleDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Mode = c.Mode
                })
                .ToListAsync();

            return Ok(formData);
        }

        /// <summary>
        /// Mise à jour partielle d'un employé (PATCH optimisé)
        /// </summary>
        [HttpPatch("{id}")]
        //[HasPermission(EDIT_EMPLOYEE)]
        public async Task<ActionResult<EmployeeReadDto>> PartialUpdate(int id, [FromBody] Dictionary<string, object> updates)
        {
            Console.WriteLine("=== DÉBUT PATCH ===");
            Console.WriteLine($"Employee ID: {id}");
            Console.WriteLine($"Nombre de champs à mettre à jour: {updates?.Count ?? 0}");

            if (updates == null || updates.Count == 0)
            {
                Console.WriteLine("❌ Aucune donnée fournie");
                return BadRequest(new { Message = "Aucune donnée de mise à jour fournie" });
            }

            // Afficher toutes les clés et valeurs reçues
            Console.WriteLine("Champs reçus:");
            foreach (var (key, value) in updates)
            {
                var valueStr = value?.ToString() ?? "null";
                var valueType = value?.GetType().Name ?? "null";
                Console.WriteLine($"  - {key}: {valueStr} (Type: {valueType})");
            }

            var userId = User.GetUserId();
            Console.WriteLine($"User ID: {userId}");

            // ===== CHARGEMENT UNIQUE DE TOUTES LES DONNÉES NÉCESSAIRES =====
            Console.WriteLine("\n=== CHARGEMENT DES DONNÉES ===");

            var currentUser = await _db.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.DeletedAt == null);

            if (currentUser?.Employee == null)
            {
                Console.WriteLine("❌ Utilisateur non associé à un employé");
                return BadRequest(new { Message = "L'utilisateur n'est pas associé à un employé" });
            }

            Console.WriteLine($"✓ Utilisateur trouvé: {currentUser.Username} (CompanyId: {currentUser.Employee.CompanyId})");

            // Charger l'employé avec TOUTES les relations en une seule requête
            Console.WriteLine($"Chargement de l'employé {id}...");

            var employee = await _db.Employees
                .Include(e => e.Status)
                .Include(e => e.Gender)
                .Include(e => e.Nationality)
                .Include(e => e.EducationLevel)
                .Include(e => e.MaritalStatus)
                .Include(e => e.Departement)
                .Include(e => e.Manager)
                .Include(e => e.Company)
                .Include(e => e.Category)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.JobPosition)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.ContractType)
                .Include(e => e.Salaries!.Where(s => s.DeletedAt == null && s.EndDate == null))
                .Include(e => e.Addresses!.Where(a => a.DeletedAt == null))
                    .ThenInclude(a => a.City)
                .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);

            if (employee == null)
            {
                Console.WriteLine($"❌ Employé {id} non trouvé");
                return NotFound(new { Message = "Employé non trouvé" });
            }

            if (currentUser?.Employee == null) return Forbid();

            var currentCompanyId = currentUser.Employee.CompanyId;

            if (employee.CompanyId <= 0) return BadRequest("Employee has no valid company.");

            if (employee.CompanyId != currentCompanyId)
            {
                int? managedByCompanyId = employee.Company?.ManagedByCompanyId;

                if (!managedByCompanyId.HasValue)
                {
                    var targetCompany = await _db.Companies
                        .AsNoTracking()
                        .Where(c => c.DeletedAt == null)
                        .Select(c => new { c.Id, c.ManagedByCompanyId })
                        .FirstOrDefaultAsync(c => c.Id == employee.CompanyId);

                    if (targetCompany == null)
                    {
                        return NotFound("Target company not found.");
                    }

                    managedByCompanyId = targetCompany.ManagedByCompanyId;
                }

                if (managedByCompanyId != currentCompanyId)
                {
                    return Forbid();
                }
            }


            var updateTime = DateTimeOffset.UtcNow;
            bool hasChanges = false;

            Console.WriteLine("\n=== TRAITEMENT DES CHAMPS SIMPLES ===");

            // ===== TRAITEMENT DES CHAMPS SIMPLES =====
            foreach (var (key, value) in updates)
            {
                Console.WriteLine($"\n→ Traitement du champ: {key}");

                var normalizedValue = value is System.Text.Json.JsonElement jsonElement
                    ? ConvertJsonElement(jsonElement, typeof(object))
                    : value;

                Console.WriteLine($"  Valeur normalisée: {normalizedValue?.ToString() ?? "null"} (Type: {normalizedValue?.GetType().Name ?? "null"})");

                var strValue = normalizedValue?.ToString();

                switch (key.ToLowerInvariant())
                {
                    case "firstname":
                        Console.WriteLine($"  FirstName actuel: {employee.FirstName}");
                        if (strValue != null && strValue != employee.FirstName)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.FirstName} → {strValue}");
                            await _eventLogService.LogSimpleEventAsync(id,
                                EmployeeEventLogService.EventNames.FirstNameChanged,
                                employee.FirstName, strValue, userId);
                            employee.FirstName = strValue;
                            hasChanges = true;
                            Console.WriteLine("  ✓ FirstName modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "lastname":
                        Console.WriteLine($"  LastName actuel: {employee.LastName}");
                        if (strValue != null && strValue != employee.LastName)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.LastName} → {strValue}");
                            await _eventLogService.LogSimpleEventAsync(id,
                                EmployeeEventLogService.EventNames.LastNameChanged,
                                employee.LastName, strValue, userId);
                            employee.LastName = strValue;
                            hasChanges = true;
                            Console.WriteLine("  ✓ LastName modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "cin":
                        Console.WriteLine($"  CIN actuel: {employee.CinNumber}");
                        if (strValue != null && strValue != employee.CinNumber)
                        {
                            Console.WriteLine("  Vérification unicité CIN...");
                            if (await _db.Employees.AnyAsync(e => e.CinNumber == strValue && e.Id != id && e.DeletedAt == null))
                            {
                                Console.WriteLine("  ❌ CIN déjà utilisé");
                                return Conflict(new { Message = "Un employé avec ce numéro CIN existe déjà" });
                            }

                            Console.WriteLine($"  → Mise à jour: {employee.CinNumber} → {strValue}");
                            await _eventLogService.LogSimpleEventAsync(id,
                                EmployeeEventLogService.EventNames.CinNumberChanged,
                                employee.CinNumber, strValue, userId);
                            employee.CinNumber = strValue;
                            hasChanges = true;
                            Console.WriteLine("  ✓ CIN modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "birthdate":
                        Console.WriteLine($"  DateOfBirth actuelle: {employee.DateOfBirth:yyyy-MM-dd}");
                        if (normalizedValue != null && DateOnly.TryParse(strValue, out var newDate) && newDate != employee.DateOfBirth)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.DateOfBirth:yyyy-MM-dd} → {newDate:yyyy-MM-dd}");
                            await _eventLogService.LogSimpleEventAsync(id,
                                EmployeeEventLogService.EventNames.DateOfBirthChanged,
                                employee.DateOfBirth.ToString("yyyy-MM-dd"),
                                newDate.ToString("yyyy-MM-dd"), userId);
                            employee.DateOfBirth = newDate;
                            hasChanges = true;
                            Console.WriteLine("  ✓ DateOfBirth modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "phone":
                        Console.WriteLine($"  Phone actuel: {employee.Phone}");
                        if (!string.IsNullOrEmpty(strValue) && strValue != employee.Phone)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.Phone} → {strValue}");
                            await _eventLogService.LogSimpleEventAsync(id,
                                EmployeeEventLogService.EventNames.PhoneChanged,
                                employee.Phone, strValue, userId);
                            employee.Phone = strValue;
                            hasChanges = true;
                            Console.WriteLine("  ✓ Phone modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "email":
                    case "personalemail":
                        Console.WriteLine($"  Email actuel: {employee.Email}");
                        if (strValue != null && strValue != employee.Email)
                        {
                            Console.WriteLine("  Vérification unicité Email...");
                            if (await _db.Employees.AnyAsync(e => e.Email == strValue && e.Id != id && e.DeletedAt == null))
                            {
                                Console.WriteLine("  ❌ Email déjà utilisé (Employees)");
                                return Conflict(new { Message = "Un employé avec cet email existe déjà" });
                            }
                            if (await _db.Users.AnyAsync(u => u.Email == strValue && u.EmployeeId != id && u.DeletedAt == null))
                            {
                                Console.WriteLine("  ❌ Email déjà utilisé (Users)");
                                return Conflict(new { Message = "Un utilisateur avec cet email existe déjà" });
                            }

                            Console.WriteLine($"  → Mise à jour: {employee.Email} → {strValue}");
                            await _eventLogService.LogSimpleEventAsync(id,
                                EmployeeEventLogService.EventNames.EmailChanged,
                                employee.Email, strValue, userId);
                            employee.Email = strValue;
                            hasChanges = true;
                            Console.WriteLine("  ✓ Email modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "cnss":
                        Console.WriteLine($"  CnssNumber actuel: {employee.CnssNumber ?? "null"}");
                        if (strValue != null && strValue != employee.CnssNumber)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.CnssNumber ?? "null"} → {strValue}");
                            await _eventLogService.LogSimpleEventAsync(id,
                                EmployeeEventLogService.EventNames.CnssNumberChanged,
                                employee.CnssNumber, strValue, userId);
                            employee.CnssNumber = strValue;
                            hasChanges = true;
                            Console.WriteLine("  ✓ CnssNumber modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "cimr":
                        Console.WriteLine($"  CimrNumber actuel: {employee.CimrNumber ?? "null"}");
                        if (strValue != null && strValue != employee.CimrNumber)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.CimrNumber ?? "null"} → {strValue}");
                            await _eventLogService.LogSimpleEventAsync(id,
                                EmployeeEventLogService.EventNames.CimrNumberChanged,
                                employee.CimrNumber, strValue, userId);
                            employee.CimrNumber = strValue;
                            hasChanges = true;
                            Console.WriteLine("  ✓ CimrNumber modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "cimremployeerate":
                        Console.WriteLine($"  CimrEmployeeRate actuel: {employee.CimrEmployeeRate?.ToString("N2") ?? "null"}");
                        if (normalizedValue != null && decimal.TryParse(strValue, out var newRate) &&
                            newRate != employee.CimrEmployeeRate)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.CimrEmployeeRate?.ToString("N2") ?? "null"} → {newRate:N2}");

                            employee.CimrEmployeeRate = newRate;
                            hasChanges = true;
                            Console.WriteLine("  ✓ CimrEmployeeRate modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "cimrcompanyrate":
                        Console.WriteLine($"  CimrCompanyRate actuel: {employee.CimrCompanyRate?.ToString("N2") ?? "null"}");
                        if (normalizedValue != null && decimal.TryParse(strValue, out var newCompRate) &&
                            newCompRate != employee.CimrCompanyRate)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.CimrCompanyRate?.ToString("N2") ?? "null"} → {newCompRate:N2}");

                            employee.CimrCompanyRate = newCompRate;
                            hasChanges = true;
                            Console.WriteLine("  ✓ CimrCompanyRate modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "hasprivateinsurance":
                        Console.WriteLine($"  HasPrivateInsurance actuel: {employee.HasPrivateInsurance}");
                        if (normalizedValue != null && bool.TryParse(strValue, out var newBool) &&
                            newBool != employee.HasPrivateInsurance)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.HasPrivateInsurance} → {newBool}");

                            employee.HasPrivateInsurance = newBool;
                            hasChanges = true;
                            Console.WriteLine("  ✓ HasPrivateInsurance modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "disableamo":
                        Console.WriteLine($"  DisableAmo actuel: {employee.DisableAmo}");
                        if (normalizedValue != null && bool.TryParse(strValue, out var newDisableAmo) &&
                            newDisableAmo != employee.DisableAmo)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.DisableAmo} → {newDisableAmo}");

                            employee.DisableAmo = newDisableAmo;
                            hasChanges = true;
                            Console.WriteLine("  ✓ DisableAmo modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "privateinsurancenumber":
                        Console.WriteLine($"  PrivateInsuranceNumber actuel: {employee.PrivateInsuranceNumber ?? "null"}");
                        if (strValue != null && strValue != employee.PrivateInsuranceNumber)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.PrivateInsuranceNumber ?? "null"} → {strValue}");
                            employee.PrivateInsuranceNumber = strValue;
                            hasChanges = true;
                            Console.WriteLine("  ✓ PrivateInsuranceNumber modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;
                    case "privateinsurancerate":
                        Console.WriteLine($"  PrivateInsuranceRate actuel: {employee.PrivateInsuranceRate?.ToString("N2") ?? "null"}");
                        if (normalizedValue != null && decimal.TryParse(strValue, out var newInsRate) &&
                            newInsRate != employee.PrivateInsuranceRate)
                        {
                            Console.WriteLine($"  → Mise à jour: {employee.PrivateInsuranceRate?.ToString("N2") ?? "null"} → {newInsRate:N2}");

                            employee.PrivateInsuranceRate = newInsRate;
                            hasChanges = true;
                            Console.WriteLine("  ✓ PrivateInsuranceRate modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    // ===== RELATIONS (IDs) =====
                    case "departementid":
                        Console.WriteLine($"  DepartementId actuel: {employee.DepartementId?.ToString() ?? "null"}");
                        if (normalizedValue != null && int.TryParse(strValue, out var deptId) && deptId != employee.DepartementId)
                        {
                            Console.WriteLine($"  Recherche du département {deptId}...");
                            var newDept = await _db.Departement.AsNoTracking()
                                .FirstOrDefaultAsync(d => d.Id == deptId && d.DeletedAt == null);
                            if (newDept == null)
                            {
                                Console.WriteLine("  ❌ Département non trouvé");
                                return NotFound(new { Message = "Département non trouvé" });
                            }
                            if (newDept.CompanyId != employee.CompanyId)
                            {
                                Console.WriteLine($"  ❌ CompanyId incompatible: {newDept.CompanyId} != {employee.CompanyId}");
                                return BadRequest(new { Message = "Le département ne correspond pas à la société de l'employé" });
                            }

                            Console.WriteLine($"  → Mise à jour: {employee.Departement?.DepartementName ?? "null"} → {newDept.DepartementName}");
                            await _eventLogService.LogRelationEventAsync(id,
                                EmployeeEventLogService.EventNames.DepartmentChanged,
                                employee.DepartementId, employee.Departement?.DepartementName,
                                deptId, newDept.DepartementName, userId);
                            employee.DepartementId = deptId;
                            hasChanges = true;
                            Console.WriteLine("  ✓ DepartementId modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "department": // Cas spécial : nom du département
                        Console.WriteLine($"  Department (par nom): {strValue}");
                        if (strValue != null)
                        {
                            Console.WriteLine($"  Recherche du département '{strValue}'...");
                            var dept = await _db.Departement.AsNoTracking()
                                .FirstOrDefaultAsync(d => d.DepartementName == strValue &&
                                    d.CompanyId == employee.CompanyId && d.DeletedAt == null);
                            if (dept != null && dept.Id != employee.DepartementId)
                            {
                                Console.WriteLine($"  → Mise à jour: {employee.Departement?.DepartementName ?? "null"} → {dept.DepartementName}");
                                await _eventLogService.LogRelationEventAsync(id,
                                    EmployeeEventLogService.EventNames.DepartmentChanged,
                                    employee.DepartementId, employee.Departement?.DepartementName,
                                    dept.Id, dept.DepartementName, userId);
                                employee.DepartementId = dept.Id;
                                hasChanges = true;
                                Console.WriteLine("  ✓ Department modifié");
                            }
                            else if (dept == null)
                            {
                                Console.WriteLine($"  ⚠ Département '{strValue}' non trouvé");
                            }
                            else
                            {
                                Console.WriteLine("  ⊘ Pas de changement");
                            }
                        }
                        break;

                    case "manager":
                        {
                            if (string.IsNullOrWhiteSpace(strValue))
                                return BadRequest(new { Message = "Nom du manager requis" });

                            var normalizedName = strValue.Trim().ToLower();

                            Console.WriteLine($"  Recherche du manager par nom: {normalizedName}");

                            var managers = await _db.Employees
                                .AsNoTracking()
                                .Where(e => e.DeletedAt == null &&
                                    (e.FirstName + " " + e.LastName).ToLower() == normalizedName)
                                .Select(e => new
                                {
                                    e.Id,
                                    FullName = e.FirstName + " " + e.LastName
                                })
                                .ToListAsync();

                            if (!managers.Any())
                                return NotFound(new { Message = "Manager non trouvé" });

                            if (managers.Count > 1)
                                return Conflict(new
                                {
                                    Message = "Nom du manager ambigu",
                                    Candidates = managers.Select(m => new { m.Id, m.FullName })
                                });

                            var newManagerId = managers[0].Id;

                            if (newManagerId == id)
                                return BadRequest(new { Message = "Un employé ne peut pas être son propre manager" });

                            if (newManagerId == employee.ManagerId)
                            {
                                Console.WriteLine("  ⊘ Pas de changement");
                                break;
                            }

                            await _eventLogService.LogRelationEventAsync(
                                id,
                                EmployeeEventLogService.EventNames.ManagerChanged,
                                employee.ManagerId,
                                employee.Manager != null
                                    ? $"{employee.Manager.FirstName} {employee.Manager.LastName}"
                                    : null,
                                newManagerId,
                                managers[0].FullName,
                                userId
                            );

                            employee.ManagerId = newManagerId;
                            hasChanges = true;

                            Console.WriteLine($"  ✓ Manager mis à jour → {managers[0].FullName}");
                            break;
                        }

                    case "status":
                        Console.WriteLine($"  Status actuel: {employee.Status?.Code ?? "null"}");

                        if (string.IsNullOrWhiteSpace(strValue))
                        {
                            Console.WriteLine("  ⊘ Valeur de statut vide — aucune modification");
                            break;
                        }

                        var incomingStatus = strValue.Trim();
                        var resolvedStatus = (Models.Referentiel.Status?)null;

                        // Si la valeur est numérique, tenter par Id
                        if (int.TryParse(incomingStatus, out var parsedStatusId))
                        {
                            Console.WriteLine($"  Recherche du statut par Id {parsedStatusId}...");
                            resolvedStatus = await _db.Statuses
                                .AsNoTracking()
                                .FirstOrDefaultAsync(s => s.Id == parsedStatusId);
                        }

                        // Sinon ou si non trouvé par Id -> rechercher par Code (insensible à la casse)
                        if (resolvedStatus == null)
                        {
                            Console.WriteLine($"  Recherche du statut par Code '{incomingStatus}'...");
                            var codeToFind = incomingStatus.ToLowerInvariant();
                            resolvedStatus = await _db.Statuses
                                .AsNoTracking()
                                .FirstOrDefaultAsync(s => s.Code.ToLower() == codeToFind);
                        }

                        if (resolvedStatus == null)
                        {
                            Console.WriteLine("  ❌ Statut non trouvé");
                            return NotFound(new { Message = "Statut non trouvé" });
                        }

                        if (resolvedStatus.Id == employee.StatusId)
                        {
                            Console.WriteLine("  ⊘ Pas de changement (même statut résolu)");
                            break;
                        }

                        Console.WriteLine($"  → Mise à jour: {employee.Status?.Code ?? "null"} → {resolvedStatus.Code}");
                        await _eventLogService.LogRelationEventAsync(
                            employeeId: id,
                            eventName: EmployeeEventLogService.EventNames.StatusChanged,
                            oldValueId: employee.StatusId,
                            oldValueName: employee.Status?.Code,
                            newValueId: resolvedStatus.Id,
                            newValueName: resolvedStatus.Code,
                            createdBy: userId
                        );

                        employee.StatusId = resolvedStatus.Id;
                        hasChanges = true;
                        Console.WriteLine("  ✓ Statut modifié");
                        break;

                    case "genderid":
                        Console.WriteLine($"  GenderId actuel: {employee.GenderId?.ToString() ?? "null"}");
                        if (normalizedValue != null && int.TryParse(strValue, out var genderId) && genderId != employee.GenderId)
                        {
                            Console.WriteLine($"  Recherche du genre {genderId}...");
                            var newGender = await _db.Genders.AsNoTracking()
                                .FirstOrDefaultAsync(g => g.Id == genderId);
                            if (newGender == null)
                            {
                                Console.WriteLine("  ❌ Genre non trouvé");
                                return NotFound(new { Message = "Genre non trouvé" });
                            }

                            Console.WriteLine($"  → Mise à jour: {employee.Gender?.Code ?? "null"} → {newGender.Code}");
                            await _eventLogService.LogRelationEventAsync(id,
                                EmployeeEventLogService.EventNames.GenderChanged,
                                employee.GenderId, employee.Gender?.Code,
                                genderId, newGender.Code, userId);
                            employee.GenderId = genderId;
                            hasChanges = true;
                            Console.WriteLine("  ✓ GenderId modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "nationalityid":
                        Console.WriteLine($"  NationalityId actuel: {employee.NationalityId?.ToString() ?? "null"}");
                        if (normalizedValue != null && int.TryParse(strValue, out var nationalityId) && nationalityId != employee.NationalityId)
                        {
                            Console.WriteLine($"  Recherche de la nationalité {nationalityId}...");
                            var newNationality = await _db.Nationalities.AsNoTracking()
                                .FirstOrDefaultAsync(n => n.Id == nationalityId && n.DeletedAt == null);
                            if (newNationality == null)
                            {
                                Console.WriteLine("  ❌ Nationalité non trouvée");
                                return NotFound(new { Message = "Nationalité non trouvée" });
                            }

                            Console.WriteLine($"  → Mise à jour: {employee.Nationality?.Name ?? "null"} → {newNationality.Name}");
                            await _eventLogService.LogRelationEventAsync(id,
                                EmployeeEventLogService.EventNames.NationalityChanged,
                                employee.NationalityId, employee.Nationality?.Name,
                                nationalityId, newNationality.Name, userId);
                            employee.NationalityId = nationalityId;
                            hasChanges = true;
                            Console.WriteLine("  ✓ NationalityId modifié");
                        }
                        else
                        {
                            Console.WriteLine("  ⊘ Pas de changement");
                        }
                        break;

                    case "educationlevel":
                        Console.WriteLine($"  EducationLevel actuel: {employee.EducationLevel?.Code ?? employee.EducationLevelId?.ToString() ?? "null"}");

                        if (string.IsNullOrWhiteSpace(strValue))
                        {
                            Console.WriteLine("  ⊘ Valeur de niveau d'éducation vide — aucune modification");
                            break;
                        }

                        var incomingEdu = strValue.Trim();
                        var resolvedEdu = (await _db.EducationLevels
                            .AsNoTracking()
                            .ToListAsync())
                            .FirstOrDefault(e =>
                                int.TryParse(incomingEdu, out var idEdu) ? e.Id == idEdu :
                                (e.Code != null && e.Code.Equals(incomingEdu, StringComparison.OrdinalIgnoreCase)) ||
                                (e.NameFr != null && e.NameFr.Equals(incomingEdu, StringComparison.OrdinalIgnoreCase)) ||
                                (e.NameEn != null && e.NameEn.Equals(incomingEdu, StringComparison.OrdinalIgnoreCase))
                            );

                        if (resolvedEdu == null)
                        {
                            Console.WriteLine("  ❌ Niveau d'éducation non trouvé");
                            return NotFound(new { Message = "Niveau d'éducation non trouvé" });
                        }

                        if (resolvedEdu.Id == employee.EducationLevelId)
                        {
                            Console.WriteLine("  ⊘ Pas de changement (même niveau résolu)");
                            break;
                        }

                        Console.WriteLine($"  → Mise à jour: {employee.EducationLevel?.Code ?? "null"} → {resolvedEdu.Code ?? resolvedEdu.Id.ToString()}");
                        await _eventLogService.LogRelationEventAsync(id,
                            EmployeeEventLogService.EventNames.EducationLevelChanged,
                            employee.EducationLevelId, employee.EducationLevel?.Code,
                            resolvedEdu.Id, resolvedEdu.Code, userId);

                        employee.EducationLevelId = resolvedEdu.Id;
                        hasChanges = true;
                        Console.WriteLine("  ✓ EducationLevel modifié");
                        break;

                    case "maritalstatus":
                        Console.WriteLine($"  MaritalStatus actuel: {employee.MaritalStatus?.Code ?? employee.MaritalStatusId?.ToString() ?? "null"}");

                        if (string.IsNullOrWhiteSpace(strValue))
                        {
                            Console.WriteLine("  ⊘ Valeur de statut matrimonial vide — aucune modification");
                            break;
                        }

                        var incomingMarital = strValue.Trim();
                        var resolvedMarital = (await _db.MaritalStatuses
                            .AsNoTracking()
                            .ToListAsync())
                            .FirstOrDefault(m =>
                                int.TryParse(incomingMarital, out var idMar) ? m.Id == idMar :
                                (m.Code != null && m.Code.Equals(incomingMarital, StringComparison.OrdinalIgnoreCase)) ||
                                (m.NameFr != null && m.NameFr.Equals(incomingMarital, StringComparison.OrdinalIgnoreCase)) ||
                                (m.NameEn != null && m.NameEn.Equals(incomingMarital, StringComparison.OrdinalIgnoreCase))
                            );

                        if (resolvedMarital == null)
                        {
                            Console.WriteLine("  ❌ Statut matrimonial non trouvé");
                            return NotFound(new { Message = "Statut matrimonial non trouvé" });
                        }

                        if (resolvedMarital.Id == employee.MaritalStatusId)
                        {
                            Console.WriteLine("  ⊘ Pas de changement (même statut résolu)");
                            break;
                        }

                        Console.WriteLine($"  → Mise à jour: {employee.MaritalStatus?.Code ?? "null"} → {resolvedMarital.Code ?? resolvedMarital.Id.ToString()}");
                        await _eventLogService.LogRelationEventAsync(id,
                            EmployeeEventLogService.EventNames.MaritalStatusChanged,
                            employee.MaritalStatusId, employee.MaritalStatus?.Code,
                            resolvedMarital.Id, resolvedMarital.Code, userId);

                        employee.MaritalStatusId = resolvedMarital.Id;
                        hasChanges = true;
                        Console.WriteLine("  ✓ MaritalStatus modifié");
                        break;

                    case "contracttype":
                        Console.WriteLine($"  ContractType actuel (contrat actif): {employee.Contracts?.FirstOrDefault(c => c.EndDate == null && c.DeletedAt == null)?.ContractType?.ContractTypeName ?? "null"}");

                        if (string.IsNullOrWhiteSpace(strValue))
                        {
                            Console.WriteLine("  ⊘ Valeur de type de contrat vide — aucune modification");
                            break;
                        }

                        var incomingCt = strValue.Trim();
                        payzen_backend.Models.Company.ContractType? matchedCt = null;

                        // Priorité : si la valeur est numérique, rechercher par Id
                        if (int.TryParse(incomingCt, out var parsedCtId))
                        {
                            Console.WriteLine($"  Recherche du type de contrat par Id {parsedCtId}...");
                            matchedCt = await _db.ContractTypes
                                .AsNoTracking()
                                .FirstOrDefaultAsync(ct => ct.Id == parsedCtId && ct.DeletedAt == null && ct.CompanyId == employee.CompanyId);
                        }

                        // Sinon, recherche par ContractTypeName (insensible à la casse)
                        if (matchedCt == null)
                        {
                            Console.WriteLine($"  Recherche du type de contrat par nom '{incomingCt}' pour la société {employee.CompanyId}...");
                            var nameToFind = incomingCt.ToLowerInvariant();
                            matchedCt = await _db.ContractTypes
                                .AsNoTracking()
                                .FirstOrDefaultAsync(ct => ct.CompanyId == employee.CompanyId
                                                           && ct.DeletedAt == null
                                                           && ct.ContractTypeName.ToLower() == nameToFind);
                        }

                        if (matchedCt == null)
                        {
                            Console.WriteLine("  ❌ Type de contrat non trouvé pour cette société");
                            return NotFound(new { Message = "Type de contrat non trouvé pour cette société" });
                        }

                        // Récupérer le contrat actif
                        var currentActiveContract = employee.Contracts?.FirstOrDefault(c => c.DeletedAt == null && c.EndDate == null);
                        if (currentActiveContract == null)
                        {
                            Console.WriteLine("  ❌ Aucun contrat actif trouvé — impossible de changer le type de contrat sans contrat actif");
                            return BadRequest(new { Message = "Aucun contrat actif trouvé. Veuillez d'abord créer un contrat." });
                        }

                        if (currentActiveContract.ContractTypeId == matchedCt.Id)
                        {
                            Console.WriteLine("  ⊘ Pas de changement (même type de contrat)");
                            break;
                        }

                        Console.WriteLine($"  → Changement de type de contrat: {currentActiveContract.ContractType?.ContractTypeName ?? "null"} → {matchedCt.ContractTypeName}");
                        // Fermer l'ancien contrat
                        currentActiveContract.EndDate = updateTime.UtcDateTime;
                        currentActiveContract.ModifiedAt = updateTime;
                        currentActiveContract.ModifiedBy = userId;

                        await _eventLogService.LogSimpleEventAsync(
                            employeeId: id,
                            eventName: EmployeeEventLogService.EventNames.ContractTerminated,
                            oldValue: $"{currentActiveContract.JobPosition?.Name} - {currentActiveContract.ContractType?.ContractTypeName}",
                            newValue: currentActiveContract.EndDate.Value.ToString("yyyy-MM-dd"),
                            createdBy: userId
                        );

                        // Créer le nouveau contrat en conservant le poste
                        var newContractForCt = new EmployeeContract
                        {
                            EmployeeId = id,
                            CompanyId = employee.CompanyId,
                            JobPositionId = currentActiveContract.JobPositionId,
                            ContractTypeId = matchedCt.Id,
                            StartDate = updateTime.UtcDateTime,
                            EndDate = null,
                            CreatedAt = updateTime,
                            CreatedBy = userId
                        };

                        _db.EmployeeContracts.Add(newContractForCt);

                        await _eventLogService.LogRelationEventAsync(
                            employeeId: id,
                            eventName: EmployeeEventLogService.EventNames.ContractTypeChanged,
                            oldValueId: currentActiveContract.ContractTypeId,
                            oldValueName: currentActiveContract.ContractType?.ContractTypeName,
                            newValueId: matchedCt.Id,
                            newValueName: matchedCt.ContractTypeName,
                            createdBy: userId
                        );

                        await _eventLogService.LogSimpleEventAsync(
                            employeeId: id,
                            eventName: EmployeeEventLogService.EventNames.ContractCreated,
                            oldValue: null,
                            newValue: $"{currentActiveContract.JobPosition?.Name} - {matchedCt.ContractTypeName}",
                            createdBy: userId
                        );

                        hasChanges = true;
                        Console.WriteLine("  ✓ ContractType modifié (nouveau contrat créé)");
                        break;

                    case "position":
                        {
                            if (string.IsNullOrWhiteSpace(strValue))
                                return BadRequest(new { Message = "Nom du poste requis" });

                            var normalizedPosition = strValue.Trim().ToLowerInvariant();

                            Console.WriteLine($"  Recherche du poste '{normalizedPosition}'");

                            var jobPositions = await _db.JobPositions
                                .AsNoTracking()
                                .Where(jp =>
                                    jp.CompanyId == employee.CompanyId &&
                                    jp.DeletedAt == null &&
                                    jp.Name.ToLower() == normalizedPosition)
                                .Select(jp => new
                                {
                                    jp.Id,
                                    jp.Name
                                })
                                .ToListAsync();

                            if (!jobPositions.Any())
                                return NotFound(new { Message = "Poste non trouvé pour cette entreprise" });

                            if (jobPositions.Count > 1)
                                return Conflict(new
                                {
                                    Message = "Nom de poste ambigu",
                                    Candidates = jobPositions
                                });

                            var newJobPosition = jobPositions[0];

                            // Charger le contrat actif en base (fiable)
                            var currentContract = await _db.EmployeeContracts
                                .Include(c => c.JobPosition)
                                .Include(c => c.ContractType)
                                .FirstOrDefaultAsync(c =>
                                    c.EmployeeId == id &&
                                    c.DeletedAt == null &&
                                    c.EndDate == null);

                            // Si aucun contrat actif : créer le premier contrat (par l'utilisateur connecté) puis logger
                            if (currentContract == null)
                            {
                                // Chercher un ContractType par défaut pour l'entreprise
                                var defaultContractType = await _db.ContractTypes
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(ct => ct.CompanyId == employee.CompanyId && ct.DeletedAt == null);

                                if (defaultContractType == null)
                                {
                                    return BadRequest(new
                                    {
                                        Message = "Aucun contrat actif et aucun type de contrat par défaut trouvé pour cette société. Veuillez créer d'abord un type de contrat."
                                    });
                                }

                                var firstContract = new EmployeeContract
                                {
                                    EmployeeId = id,
                                    CompanyId = employee.CompanyId,
                                    JobPositionId = newJobPosition.Id,
                                    ContractTypeId = defaultContractType.Id,
                                    StartDate = DateTime.UtcNow,
                                    EndDate = null,
                                    CreatedAt = updateTime,
                                    CreatedBy = userId
                                };

                                _db.EmployeeContracts.Add(firstContract);
                                await _db.SaveChangesAsync();

                                // Logger l'affectation du poste (relation) - ancien manager/position absent
                                await _eventLogService.LogRelationEventAsync(
                                    employeeId: id,
                                    eventName: EmployeeEventLogService.EventNames.JobPositionChanged,
                                    oldValueId: null,
                                    oldValueName: null,
                                    newValueId: newJobPosition.Id,
                                    newValueName: newJobPosition.Name,
                                    createdBy: userId
                                );

                                // Logger la création du contrat
                                await _eventLogService.LogSimpleEventAsync(
                                    employeeId: id,
                                    eventName: EmployeeEventLogService.EventNames.ContractCreated,
                                    oldValue: null,
                                    newValue: $"{newJobPosition.Name} - {defaultContractType.ContractTypeName}",
                                    createdBy: userId
                                );

                                hasChanges = true;
                                Console.WriteLine("  ✓ Premier contrat créé et événements enregistrés");
                                break;
                            }

                            if (currentContract.JobPositionId == newJobPosition.Id)
                            {
                                Console.WriteLine("  ⊘ Pas de changement");
                                break;
                            }

                            Console.WriteLine($"  → Changement de poste: {currentContract.JobPosition?.Name} → {newJobPosition.Name}");

                            // Fermer l'ancien contrat
                            currentContract.EndDate = DateTime.UtcNow;
                            currentContract.ModifiedAt = updateTime;
                            currentContract.ModifiedBy = userId;

                            await _eventLogService.LogSimpleEventAsync(
                                employeeId: id,
                                eventName: EmployeeEventLogService.EventNames.ContractTerminated,
                                oldValue: $"{currentContract.JobPosition?.Name} - {currentContract.ContractType?.ContractTypeName}",
                                newValue: currentContract.EndDate.Value.ToString("yyyy-MM-dd"),
                                createdBy: userId
                            );

                            // Créer le nouveau contrat
                            var newContract = new EmployeeContract
                            {
                                EmployeeId = id,
                                CompanyId = employee.CompanyId,
                                JobPositionId = newJobPosition.Id,
                                ContractTypeId = currentContract.ContractTypeId,
                                StartDate = DateTime.UtcNow,
                                CreatedAt = updateTime,
                                CreatedBy = userId
                            };

                            _db.EmployeeContracts.Add(newContract);

                            await _eventLogService.LogRelationEventAsync(
                                employeeId: id,
                                eventName: EmployeeEventLogService.EventNames.JobPositionChanged,
                                oldValueId: currentContract.JobPositionId,
                                oldValueName: currentContract.JobPosition?.Name,
                                newValueId: newJobPosition.Id,
                                newValueName: newJobPosition.Name,
                                createdBy: userId
                            );

                            await _eventLogService.LogSimpleEventAsync(
                                employeeId: id,
                                eventName: EmployeeEventLogService.EventNames.ContractCreated,
                                oldValue: null,
                                newValue: $"{newJobPosition.Name} - {currentContract.ContractType?.ContractTypeName}",
                                createdBy: userId
                            );

                            hasChanges = true;
                            Console.WriteLine("  ✓ Poste modifié");
                            break;
                        }

                    case "startdate":
                        {
                            if (string.IsNullOrWhiteSpace(strValue))
                                return BadRequest(new { Message = "StartDate requis" });

                            if (!DateTime.TryParse(strValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var newStartDate))
                                return BadRequest(new { Message = "Format de date invalide" });

                            newStartDate = newStartDate.Date;

                            // Charger le contrat actif
                            var currentContract = await _db.EmployeeContracts
                                .AsTracking()
                                .Include(c => c.JobPosition)
                                .Include(c => c.ContractType)
                                .FirstOrDefaultAsync(c =>
                                    c.EmployeeId == id &&
                                    c.DeletedAt == null &&
                                    c.EndDate == null);

                            if (currentContract == null)
                                return Conflict(new { Message = "Aucun contrat actif trouvé" });

                            if (currentContract.StartDate.Date == newStartDate)
                            {
                                Console.WriteLine("  ⊘ StartDate identique");
                                break;
                            }

                            if (currentContract.StartDate <= DateTime.UtcNow.Date)
                                return Conflict(new
                                {
                                    Message = "Impossible de modifier la date de début d'un contrat déjà effectif"
                                });


                            Console.WriteLine($"  → Changement StartDate: {currentContract.StartDate:yyyy-MM-dd} → {newStartDate:yyyy-MM-dd}");

                            await _eventLogService.LogSimpleEventAsync(
                                employeeId: id,
                                eventName: EmployeeEventLogService.EventNames.ContractStartDateChanged,
                                oldValue: currentContract.StartDate.ToString("yyyy-MM-dd"),
                                newValue: newStartDate.ToString("yyyy-MM-dd"),
                                createdBy: userId
                            );

                            currentContract.StartDate = newStartDate;
                            currentContract.ModifiedAt = updateTime;
                            currentContract.ModifiedBy = userId;

                            hasChanges = true;
                            Console.WriteLine("  ✓ StartDate modifiée");
                            break;
                        }

                    case "categoryid":
                    case "category":
                        if (key == "categoryId" && normalizedValue != null && int.TryParse(strValue, out var catId))
                        {
                            if (catId != employee.CategoryId)
                            {
                                var newCategory = await _db.EmployeeCategories
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.Id == catId && c.DeletedAt == null);

                                if (newCategory == null)
                                {
                                    return NotFound(new { Message = "Catégorie d'employé non trouvée" });
                                }

                                if (newCategory.CompanyId != employee.CompanyId)
                                {
                                    return BadRequest(new { Message = "La catégorie ne correspond pas à la société de l'employé" });
                                }

                                await _eventLogService.LogRelationEventAsync(id,
                                    "Category_Changed",
                                    employee.CategoryId, employee.Category?.Name,
                                    catId, newCategory.Name, userId);

                                employee.CategoryId = catId;
                                hasChanges = true;
                                Console.WriteLine("  ✓ CategoryId modifié");
                            }
                            else
                            {
                                Console.WriteLine("  ⊘ Pas de changement");
                            }
                        }
                        else if (key == "category" && strValue != null)
                        {
                            var cat = await _db.EmployeeCategories
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.Name.ToLower() == strValue.ToLower() &&
                                    c.CompanyId == employee.CompanyId && c.DeletedAt == null);

                            if (cat != null && cat.Id != employee.CategoryId)
                            {
                                Console.WriteLine($"  → Mise à jour: {employee.Category?.Name ?? "null"} → {cat.Name}");
                                await _eventLogService.LogRelationEventAsync(id,
                                    "Category_Changed",
                                    employee.CategoryId, employee.Category?.Name,
                                    cat.Id, cat.Name, userId);

                                employee.CategoryId = cat.Id;
                                hasChanges = true;
                                Console.WriteLine("  ✓ Category modifiée");
                            }
                            else if (cat == null)
                            {
                                Console.WriteLine($"  ⚠ Catégorie '{strValue}' non trouvée");
                            }
                            else
                            {
                                Console.WriteLine("  ⊘ Pas de changement");
                            }
                        }
                        break;

                    default:
                        Console.WriteLine($"  ⚠ Champ non reconnu: {key}");
                        break;
                }
            }

            // ===== GESTION DE L'ADRESSE =====
            if (updates.ContainsKey("addressLine1") || updates.ContainsKey("addressLine2") ||
                updates.ContainsKey("cityId") || updates.ContainsKey("zipCode"))
            {
                Console.WriteLine("\n=== GESTION DE L'ADRESSE ===");

                var activeAddress = employee.Addresses?.FirstOrDefault(a => a.DeletedAt == null);
                Console.WriteLine($"Adresse active existante: {(activeAddress != null ? "OUI" : "NON")}");

                if (activeAddress != null)
                {
                    Console.WriteLine($"  Adresse actuelle: {activeAddress.AddressLine1}, {activeAddress.ZipCode}, {activeAddress.City?.CityName}");
                    bool addressModified = false;

                    var oldAddressLine1 = activeAddress.AddressLine1;
                    var oldAddressLine2 = activeAddress.AddressLine2;
                    var oldZipCode = activeAddress.ZipCode;
                    var oldCityId = activeAddress.CityId;
                    var oldCityName = activeAddress.City?.CityName;

                    // Mettre à jour uniquement les champs fournis
                    if (updates.ContainsKey("addressLine1"))
                    {
                        var newAddressLine1 = updates["addressLine1"]?.ToString();
                        Console.WriteLine($"  → AddressLine1: {activeAddress.AddressLine1} → {newAddressLine1}");
                        if (!string.IsNullOrEmpty(newAddressLine1) && newAddressLine1 != activeAddress.AddressLine1)
                        {
                            activeAddress.AddressLine1 = newAddressLine1;
                            addressModified = true;
                            Console.WriteLine("    ✓ Modifié");
                        }
                    }

                    if (updates.ContainsKey("addressLine2"))
                    {
                        var newAddressLine2 = updates["addressLine2"]?.ToString();
                        Console.WriteLine($"  → AddressLine2: {activeAddress.AddressLine2 ?? "null"} → {newAddressLine2 ?? "null"}");
                        if (newAddressLine2 != activeAddress.AddressLine2)
                        {
                            activeAddress.AddressLine2 = newAddressLine2;
                            addressModified = true;
                            Console.WriteLine("    ✓ Modifié");
                        }
                    }

                    if (updates.ContainsKey("zipCode"))
                    {
                        var newZipCode = updates["zipCode"]?.ToString();
                        Console.WriteLine($"  → ZipCode: {activeAddress.ZipCode} → {newZipCode}");
                        if (!string.IsNullOrEmpty(newZipCode) && newZipCode != activeAddress.ZipCode)
                        {
                            activeAddress.ZipCode = newZipCode;
                            addressModified = true;
                            Console.WriteLine("    ✓ Modifié");
                        }
                    }

                    if (updates.ContainsKey("cityId"))
                    {
                        if (int.TryParse(updates["cityId"]?.ToString(), out var newCityId))
                        {
                            Console.WriteLine($"  → CityId: {activeAddress.CityId} → {newCityId}");
                            if (newCityId != activeAddress.CityId)
                            {
                                var city = await _db.Cities.AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.Id == newCityId && c.DeletedAt == null);

                                if (city == null)
                                {
                                    Console.WriteLine("    ❌ Ville non trouvée");
                                    return NotFound(new { Message = "Ville non trouvée" });
                                }

                                activeAddress.CityId = newCityId;
                                addressModified = true;
                                Console.WriteLine("    ✓ Modifié");
                            }
                        }
                    }

                    if (addressModified)
                    {
                        activeAddress.ModifiedAt = updateTime;
                        activeAddress.ModifiedBy = userId;

                        // Ancienne valeur
                        var oldAddressFormatted = $"{oldAddressLine1}{(!string.IsNullOrEmpty(oldAddressLine2) ? $", {oldAddressLine2}" : "")}, {oldZipCode}, {oldCityName}";

                        // Nouvelle valeur
                        var newCityName = await _db.Cities
                            .AsNoTracking()
                            .Where(c => c.Id == activeAddress.CityId)
                            .Select(c => c.CityName)
                            .FirstOrDefaultAsync();

                        var newAddressFormatted = $"{activeAddress.AddressLine1}{(!string.IsNullOrEmpty(activeAddress.AddressLine2) ? $", {activeAddress.AddressLine2}" : "")}, {activeAddress.ZipCode}, {newCityName}";

                        Console.WriteLine($"  Log événement:");
                        Console.WriteLine($"    Ancien: {oldAddressFormatted}");
                        Console.WriteLine($"    Nouveau: {newAddressFormatted}");

                        await _eventLogService.LogSimpleEventAsync(id,
                            EmployeeEventLogService.EventNames.AddressUpdated,
                            oldAddressFormatted,
                            newAddressFormatted,
                            userId);

                        hasChanges = true;
                        Console.WriteLine("  ✓ Adresse modifiée avec succès");
                    }
                    else
                    {
                        Console.WriteLine("  ⊘ Aucune modification d'adresse");
                    }
                }
                else
                {
                    // Créer une nouvelle adresse
                    var addressLine1 = updates.ContainsKey("addressLine1") ? updates["addressLine1"]?.ToString() : null;
                    var cityId = updates.ContainsKey("cityId") && int.TryParse(updates["cityId"]?.ToString(), out var cId) ? (int?)cId : null;
                    var zipCode = updates.ContainsKey("zipCode") ? updates["zipCode"]?.ToString() : null;

                    Console.WriteLine($"  Création nouvelle adresse:");
                    Console.WriteLine($"    AddressLine1: {addressLine1 ?? "null"}");
                    Console.WriteLine($"    CityId: {cityId?.ToString() ?? "null"}");
                    Console.WriteLine($"    ZipCode: {zipCode ?? "null"}");

                    if (!string.IsNullOrEmpty(addressLine1) && cityId.HasValue && !string.IsNullOrEmpty(zipCode))
                    {
                        var city = await _db.Cities.AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Id == cityId && c.DeletedAt == null);

                        if (city == null)
                        {
                            Console.WriteLine("  ❌ Ville non trouvée");
                            return NotFound(new { Message = "Ville non trouvée" });
                        }

                        var addressLine2 = updates.ContainsKey("addressLine2") ? updates["addressLine2"]?.ToString() : null;

                        var newAddress = new EmployeeAddress
                        {
                            EmployeeId = id,
                            CityId = cityId.Value,
                            AddressLine1 = addressLine1,
                            AddressLine2 = addressLine2,
                            ZipCode = zipCode,
                            CreatedAt = updateTime,
                            CreatedBy = userId
                        };

                        _db.EmployeeAddresses.Add(newAddress);

                        var newAddressFormatted = $"{addressLine1}{(!string.IsNullOrEmpty(addressLine2) ? $", {addressLine2}" : "")}, {zipCode}, {city.CityName}";
                        Console.WriteLine($"  Nouvelle adresse: {newAddressFormatted}");

                        await _eventLogService.LogSimpleEventAsync(id,
                            EmployeeEventLogService.EventNames.AddressCreated,
                            null,
                            newAddressFormatted,
                            userId);

                        hasChanges = true;
                        Console.WriteLine("  ✓ Nouvelle adresse créée");
                    }
                    else
                    {
                        Console.WriteLine("  ⚠ Création impossible : données incomplètes");
                    }
                }

                Console.WriteLine("=== FIN GESTION ADRESSE ===");
            }

            // ===== GESTION DU SALAIRE =====
            if (updates.ContainsKey("salary") || updates.ContainsKey("baseSalary"))
            {
                Console.WriteLine("\n=== GESTION DU SALAIRE ===");

                var salaryKey = updates.ContainsKey("salary") ? "salary" : "baseSalary";
                Console.WriteLine($"Clé utilisée: {salaryKey}");

                var rawSalaryString = updates[salaryKey]?.ToString();

                // Accepter à la fois les formats "fr" (28 100,40) et "invariant" (28100.40)
                // 1) normaliser les espaces insécables éventuels
                rawSalaryString = string.IsNullOrWhiteSpace(rawSalaryString)
                    ? rawSalaryString
                    : rawSalaryString.Replace('\u00A0', ' ');

                var numberStyles = System.Globalization.NumberStyles.Number;
                var frCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
                var invariant = System.Globalization.CultureInfo.InvariantCulture;

                decimal newSalary;
                var canParse =
                    decimal.TryParse(rawSalaryString, numberStyles, frCulture, out newSalary) ||
                    decimal.TryParse(rawSalaryString, numberStyles, invariant, out newSalary);

                if (canParse)
                {
                    Console.WriteLine($"Nouveau salaire: {newSalary:N2}");

                    var activeSalary = employee.Salaries?.FirstOrDefault(s => s.DeletedAt == null && s.EndDate == null);
                    Console.WriteLine($"Salaire actif existant: {(activeSalary != null ? $"{activeSalary.BaseSalary:N2}" : "AUCUN")}");

                    if (activeSalary == null || activeSalary.BaseSalary != newSalary)
                    {
                        var activeContract = employee.Contracts?.FirstOrDefault(c => c.DeletedAt == null && c.EndDate == null);
                        Console.WriteLine($"Contrat actif: {(activeContract != null ? $"ID {activeContract.Id}" : "AUCUN")}");

                        if (activeContract == null)
                        {
                            Console.WriteLine("  ❌ Aucun contrat actif");
                            return BadRequest(new { Message = "Aucun contrat actif. Veuillez d'abord créer un contrat." });
                        }

                        var effectiveDate = updates.ContainsKey("salaryEffectiveDate") && DateTime.TryParse(updates["salaryEffectiveDate"]?.ToString(), out var sed)
                            ? sed
                            : DateTime.UtcNow;

                        Console.WriteLine($"Date effective: {effectiveDate:yyyy-MM-dd}");

                        // Fermer l'ancien salaire
                        if (activeSalary != null)
                        {
                            Console.WriteLine($"  Fermeture ancien salaire (ID {activeSalary.Id})");
                            activeSalary.EndDate = effectiveDate;
                            activeSalary.ModifiedAt = updateTime;
                            activeSalary.ModifiedBy = userId;
                        }

                        // Créer le nouveau salaire
                        var newSalaryEntity = new EmployeeSalary
                        {
                            EmployeeId = id,
                            ContractId = activeContract.Id,
                            BaseSalary = newSalary,
                            EffectiveDate = effectiveDate,
                            EndDate = null,
                            CreatedAt = updateTime,
                            CreatedBy = userId
                        };

                        _db.EmployeeSalaries.Add(newSalaryEntity);
                        Console.WriteLine($"  Nouveau salaire créé: {newSalary:N2}");

                        await _eventLogService.LogSimpleEventAsync(id,
                            EmployeeEventLogService.EventNames.SalaryUpdated,
                            activeSalary?.BaseSalary.ToString("N2"),
                            newSalary.ToString("N2"), userId);

                        hasChanges = true;
                        Console.WriteLine("  ✓ Salaire modifié avec succès");
                    }
                    else
                    {
                        Console.WriteLine("  ⊘ Pas de changement de salaire");
                    }
                }
                else
                {
                    Console.WriteLine("  ⚠ Valeur de salaire invalide");
                }

                Console.WriteLine("=== FIN GESTION SALAIRE ===");
            }

            // ===== SAUVEGARDER SI CHANGEMENTS =====
            Console.WriteLine($"\n=== SAUVEGARDE ===");
            Console.WriteLine($"Changements détectés: {hasChanges}");

            if (hasChanges)
            {
                employee.ModifiedAt = updateTime;
                employee.ModifiedBy = userId;

                try
                {
                    await _db.SaveChangesAsync();
                    Console.WriteLine("✓ Modifications sauvegardées");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ERREUR SAUVEGARDE: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine("⊘ Aucune modification à sauvegarder");
            }

            // ===== RETOURNER L'EMPLOYÉ MIS À JOUR =====
            Console.WriteLine("\n=== RÉCUPÉRATION EMPLOYÉ MIS À JOUR ===");

            var updatedEmployee = await _db.Employees
                .AsNoTracking()
                .Include(e => e.Company)
                .Include(e => e.Manager)
                .Include(e => e.Departement)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.JobPosition)
                .Include(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                    .ThenInclude(c => c.ContractType)
                .FirstAsync(e => e.Id == id);

            Console.WriteLine($"✓ Employé récupéré: {updatedEmployee.FirstName} {updatedEmployee.LastName}");

            var contract = updatedEmployee.Contracts?
                .Where(c => c.DeletedAt == null && c.EndDate == null)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefault();

            var result = new EmployeeReadDto
            {
                Id = updatedEmployee.Id,
                FirstName = updatedEmployee.FirstName,
                LastName = updatedEmployee.LastName,
                CinNumber = updatedEmployee.CinNumber,
                DateOfBirth = updatedEmployee.DateOfBirth,
                Phone = updatedEmployee.Phone,
                Email = updatedEmployee.Email,
                CompanyId = updatedEmployee.CompanyId,
                CompanyName = updatedEmployee.Company?.CompanyName ?? "",
                DepartementId = updatedEmployee.DepartementId,
                DepartementName = updatedEmployee.Departement?.DepartementName,
                ManagerId = updatedEmployee.ManagerId,
                ManagerName = updatedEmployee.Manager != null
                    ? $"{updatedEmployee.Manager.FirstName} {updatedEmployee.Manager.LastName}"
                    : null,
                StatusId = updatedEmployee.StatusId,
                GenderId = updatedEmployee.GenderId,
                EducationLevelId = updatedEmployee.EducationLevelId,
                MaritalStatusId = updatedEmployee.MaritalStatusId,
                JobPostionName = contract?.JobPosition?.Name,
                CreatedAt = updatedEmployee.CreatedAt.DateTime
            };

            Console.WriteLine("✓ DTO créé");
            Console.WriteLine("=== FIN PATCH ===\n");

            return Ok(result);
        }

        /// <summary>
        /// Convertit un JsonElement en type .NET approprié
        /// </summary>
        private static object? ConvertJsonElement(System.Text.Json.JsonElement element, Type targetType)
        {
            switch (element.ValueKind)
            {
                case System.Text.Json.JsonValueKind.Null:
                    return null;
                case System.Text.Json.JsonValueKind.String:
                    var stringValue = element.GetString();
                    if (targetType == typeof(DateTime))
                        return DateTime.Parse(stringValue!);
                    if (targetType == typeof(DateTimeOffset))
                        return DateTimeOffset.Parse(stringValue!);
                    return stringValue;
                case System.Text.Json.JsonValueKind.Number:
                    if (targetType == typeof(int))
                        return element.GetInt32();
                    if (targetType == typeof(decimal))
                        return element.GetDecimal();
                    if (targetType == typeof(double))
                        return element.GetDouble();
                    if (targetType == typeof(long))
                        return element.GetInt64();
                    return element.GetDecimal();
                case System.Text.Json.JsonValueKind.True:
                    return true;
                case System.Text.Json.JsonValueKind.False:
                    return false;
                default:
                    return element.ToString();
            }
        }

        /// <summary>
        /// Récupère l'historique des modifications d'un employé
        /// </summary>
        [HttpGet("{id}/history")]
        //[HasPermission(VIEW_EMPLOYEE)]
        public async Task<ActionResult<IEnumerable<EmployeeEventLog>>> GetEmployeeHistory(int id)
        {
            var employeeExists = await _db.Employees.AnyAsync(e => e.Id == id && e.DeletedAt == null);
            if (!employeeExists)
                return NotFound(new { Message = "Employé non trouvé" });

            var history = await _db.EmployeeEventLogs
                .Where(e => e.employeeId == id)
                .OrderByDescending(e => e.createdAt)
                .ToListAsync();

            return Ok(history);
        }

        /// <summary>
        /// Récupère tous les employés avec informations simplifiées
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeSimpleDto>>> GetAllEmployees()
        {
            Console.WriteLine("=== GET ALL EMPLOYEES (optimized, returning UserId) ===");

            // 1) Récupérer la map EmployeeId -> UserId (utilisateurs actifs / non supprimés)
            var usersWithEmployee = await _db.Users
                .AsNoTracking()
                .Where(u => u.DeletedAt == null && u.EmployeeId != null && u.IsActive)
                .Select(u => new { UserId = u.Id, EmployeeId = u.EmployeeId!.Value })
                .ToListAsync();

            var userIdByEmployee = usersWithEmployee
                .GroupBy(x => x.EmployeeId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).First()); // si plusieurs, prendre le premier

            // 2) Récupérer les rôles par employee (UsersRoles -> Users -> Roles)
            var userRoles = await (from ur in _db.UsersRoles.AsNoTracking()
                                   where ur.DeletedAt == null
                                   join u in _db.Users.AsNoTracking().Where(u => u.DeletedAt == null && u.IsActive)
                                       on ur.UserId equals u.Id
                                   join r in _db.Roles.AsNoTracking().Where(r => r.DeletedAt == null)
                                       on ur.RoleId equals r.Id
                                   select new { EmployeeId = u.EmployeeId, RoleName = r.Name })
                                  .ToListAsync();

            var rolesByEmployee = userRoles
                .Where(x => x.EmployeeId.HasValue)
                .GroupBy(x => x.EmployeeId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName ?? string.Empty).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

            // 3) Récupérer les employés en une seule requête
            var employees = await _db.Employees
                .AsNoTracking()
                .Where(e => e.DeletedAt == null)
                .Include(e => e.Company)
                .Include(e => e.Status)
                .OrderBy(e => e.Company!.CompanyName)
                .ThenBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            // 4) Projeter vers DTOs en joignant les rôles et en remplaçant l'Id par le UserId (0 si aucun user)
            var result = employees.Select(e =>
            {
                var userId = userIdByEmployee.TryGetValue(e.Id, out var uid) ? uid : 0;
                return new EmployeeSimpleDto
                {
                    Id = userId, // <-- retourne maintenant le UserId (ou 0 si pas de compte)
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    CompanyName = e.Company?.CompanyName ?? string.Empty,
                    Email = e.Email,
                    Phone = e.Phone,
                    RoleNames = rolesByEmployee.TryGetValue(e.Id, out var list) ? list : new List<string>(),
                    statuses = e.Status?.Code
                };
            }).ToList();

            Console.WriteLine($"Nombre d'employés retournés: {result.Count}");
            return Ok(result);
        }

        /// <summary>
        /// Récupère les informations de l'employé connecté
        /// </summary>
        [HttpGet("current")]
        [Authorize]
        public async Task<ActionResult<EmployeeDto>> GetCurrentEmployee()
        {
            var userId = User.GetUserId();

            // Trouver l'employé via la relation Users -> Employee
            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Departement)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Contracts!.Where(c => c.DeletedAt == null && c.EndDate == null))
                        .ThenInclude(c => c.JobPosition)
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive);

            if (user?.Employee == null)
                return NotFound(new { Message = "Aucun employé trouvé pour cet utilisateur" });

            var employee = user.Employee;

            // Récupérer le contrat actif pour obtenir le poste
            var activeContract = employee.Contracts?
                .FirstOrDefault(c => c.DeletedAt == null && c.EndDate == null);

            return Ok(new EmployeeDto
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                FullName = $"{employee.FirstName} {employee.LastName}",
                DepartementName = employee.Departement?.DepartementName
            });
        }
        /// <summary>
        /// Génère un matricule unique pour un employé dans une société donnée
        /// </summary>
        /// <param name="companyId">ID de la société</param>
        /// <returns>Matricule unique</returns>
        private async Task<int?> GenerateUniqueMatricule(int companyId)
        {
            // Récupérer le matricule maximum actuel pour la société
            var maxMatricule = await _db.Employees
                .Where(e => e.CompanyId == companyId && e.DeletedAt == null && e.Matricule.HasValue)
                .MaxAsync(e => (int?)e.Matricule);

            // Si aucun matricule n'existe, commencer à 1, sinon incrémenter
            return (maxMatricule ?? 0) + 1;
        }
    }
}
