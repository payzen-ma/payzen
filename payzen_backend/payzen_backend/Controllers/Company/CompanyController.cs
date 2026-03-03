using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Authorization;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Company.Dtos;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Referentiel;
using payzen_backend.Models.Users;
using payzen_backend.Services;
using payzen_backend.Services.Company.Interfaces;
using System;
using System.Linq;
using System.Text.Json;

namespace payzen_backend.Controllers.Company
{
    [Route("api/companies")]
    [ApiController]
    [Authorize]
    public class CompanyController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PasswordGeneratorService _passwordGenerator;
        private readonly CompanyEventLogService _companyEventLogService;
        private readonly EmployeeEventLogService _employeeEventLogService;
        private readonly ICompanyOnboardingService _companyOnboardingService;

        public CompanyController(
            AppDbContext db,
            PasswordGeneratorService passwordGenerator,
            CompanyEventLogService companyEventLogService,
            EmployeeEventLogService employeeEventLogService,
            ICompanyOnboardingService companyOnboardingService)
        {
            _db = db;
            _passwordGenerator = passwordGenerator;
            _companyEventLogService = companyEventLogService;
            _employeeEventLogService = employeeEventLogService;
            _companyOnboardingService = companyOnboardingService;
        }

        /// <summary>
        /// Récupère la liste de toutes les entreprises
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompanyListDto>>> GetAllCompanies()
        {
            var companies = await _db.Companies
                .AsNoTracking()
                .Where(c => c.DeletedAt == null)
                .Include(c => c.City)
                .Include(c => c.Country)
                .OrderBy(c => c.CompanyName)
                .Select(c => new CompanyListDto
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    IsCabinetExpert = c.IsCabinetExpert,
                    Email = c.Email,
                    CountryPhoneCode = c.Country != null ? c.Country.CountryPhoneCode : null,
                    PhoneNumber = c.PhoneNumber,
                    CityName = c.City != null ? c.City.CityName : null,
                    CountryName = c.Country != null ? c.Country.CountryName : null,
                    CnssNumber = c.CnssNumber,
                    PatenteNumber = c.PatenteNumber,
                    RibNumber = c.RibNumber,
                    WebsiteUrl = c.WebsiteUrl,
                    isActive = c.isActive,
                    CreatedAt = c.CreatedAt.DateTime
                })
                .ToListAsync();

            return Ok(companies);
        }

        /// <summary>
        /// Récupère une entreprise par son ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CompanyListDto>> GetCompanyById(int id)
        {
            var company = await _db.Companies
                .AsNoTracking()
                .Where(c => c.Id == id && c.DeletedAt == null)
                .Include(c => c.City)
                .Include(c => c.Country)
                .Select(c => new CompanyListDto
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    IsCabinetExpert = c.IsCabinetExpert,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber,
                    CountryPhoneCode = c.Country != null ? c.Country.CountryPhoneCode : null,
                    CityName = c.City != null ? c.City.CityName : null,
                    CountryName = c.Country != null ? c.Country.CountryName : null,
                    CompanyAddress = c.CompanyAddress != null ? c.CompanyAddress : null,
                    IceNumber = c.IceNumber,
                    IfNumber = c.IfNumber,
                    RcNumber = c.RcNumber,
                    PatenteNumber = c.PatenteNumber,
                    WebsiteUrl = c.WebsiteUrl,
                    RibNumber = c.RibNumber,
                    LegalForm = c.LegalForm,
                    FoundingDate = c.FoundingDate,
                    isActive = c.isActive,
                    CnssNumber = c.CnssNumber,

                    CreatedAt = c.CreatedAt.DateTime
                })
                .FirstOrDefaultAsync();

            if (company == null)
                return NotFound(new { Message = "Entreprise non trouvée" });

            return Ok(company);
        }

        /// <summary>
        /// Récupère les entreprises par ville
        /// </summary>
        [HttpGet("by-city/{cityId}")]
        public async Task<ActionResult<IEnumerable<CompanyListDto>>> GetCompaniesByCity(int cityId)
        {
            var cityExists = await _db.Cities.AnyAsync(c => c.Id == cityId && c.DeletedAt == null);
            if (!cityExists)
                return NotFound(new { Message = "Ville non trouvée" });

            var companies = await _db.Companies
                .AsNoTracking()
                .Where(c => c.DeletedAt == null && c.CityId == cityId)
                .Include(c => c.City)
                .Include(c => c.Country)
                .OrderBy(c => c.CompanyName)
                .Select(c => new CompanyListDto
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    IsCabinetExpert = c.IsCabinetExpert,
                    Email = c.Email,
                    CountryPhoneCode = c.Country != null ? c.Country.CountryPhoneCode : null,
                    CityName = c.City != null ? c.City.CityName : null,
                    CountryName = c.Country != null ? c.Country.CountryName : null,
                    CnssNumber = c.CnssNumber,
                    RibNumber = c.RibNumber,
                    isActive = c.isActive,
                    CreatedAt = c.CreatedAt.DateTime
                })
                .ToListAsync();

            return Ok(companies);
        }

        /// <summary>
        /// Récupère les entreprises par pays
        /// </summary>
        [HttpGet("by-country/{countryId}")]
        public async Task<ActionResult<IEnumerable<CompanyListDto>>> GetCompaniesByCountry(int countryId)
        {
            var countryExists = await _db.Countries.AnyAsync(c => c.Id == countryId && c.DeletedAt == null);
            if (!countryExists)
                return NotFound(new { Message = "Pays non trouvé" });

            var companies = await _db.Companies
                .AsNoTracking()
                .Where(c => c.DeletedAt == null && c.CountryId == countryId)
                .Include(c => c.City)
                .Include(c => c.Country)
                .OrderBy(c => c.CompanyName)
                .Select(c => new CompanyListDto
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    IsCabinetExpert = c.IsCabinetExpert,
                    Email = c.Email,
                    CountryPhoneCode = c.Country != null ? c.Country.CountryPhoneCode : null,
                    CityName = c.City != null ? c.City.CityName : null,
                    CountryName = c.Country != null ? c.Country.CountryName : null,
                    CnssNumber = c.CnssNumber,
                    CreatedAt = c.CreatedAt.DateTime
                })
                .ToListAsync();

            return Ok(companies);
        }

        /// <summary>
        /// Recherche d'entreprises par nom
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CompanyListDto>>> SearchCompanies([FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest(new { Message = "Le terme de recherche est requis" });

            var companies = await _db.Companies
                .AsNoTracking()
                .Where(c => c.DeletedAt == null &&
                            (c.CompanyName.Contains(searchTerm) ||
                             c.Email.Contains(searchTerm) ||
                             c.CnssNumber.Contains(searchTerm)))
                .Include(c => c.City)
                .Include(c => c.Country)
                .OrderBy(c => c.CompanyName)
                .Select(c => new CompanyListDto
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    IsCabinetExpert = c.IsCabinetExpert,
                    Email = c.Email,
                    CountryPhoneCode = c.Country != null ? c.Country.CountryPhoneCode : null,
                    CityName = c.City != null ? c.City.CityName : null,
                    CountryName = c.Country != null ? c.Country.CountryName : null,
                    CnssNumber = c.CnssNumber,
                    CreatedAt = c.CreatedAt.DateTime
                })
                .ToListAsync();

            return Ok(companies);
        }

        /// <summary>
        /// Récupère toutes les données nécessaires pour le formulaire de création d'entreprise
        /// </summary>
        [HttpGet("form-data")]
        public async Task<ActionResult<CompanyFormDataDto>> GetFormData()
        {
            var formData = new CompanyFormDataDto();

            // 1. Récupérer tous les pays
            formData.Countries = await _db.Countries
                .Where(c => c.DeletedAt == null)
                .OrderBy(c => c.CountryName)
                .Select(c => new CountryFormDto
                {
                    Id = c.Id,
                    CountryName = c.CountryName,
                    CountryNameAr = c.CountryNameAr,
                    CountryCode = c.CountryCode,
                    CountryPhoneCode = c.CountryPhoneCode
                })
                .ToListAsync();

            // 2. Récupérer toutes les villes avec leur pays
            formData.Cities = await _db.Cities
                .Where(c => c.DeletedAt == null)
                .Include(c => c.Country)
                .OrderBy(c => c.Country!.CountryName)
                .ThenBy(c => c.CityName)
                .Select(c => new CityFormDto
                {
                    Id = c.Id,
                    CityName = c.CityName,
                    CountryId = c.CountryId,
                    CountryName = c.Country != null ? c.Country.CountryName : null
                })
                .ToListAsync();

            return Ok(formData);
        }

        /// <summary>
        /// Crée une nouvelle entreprise avec son administrateur
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CompanyCreateResponseDto>> CreateCompany([FromBody] CompanyCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ===== VALIDATIONS PERSONNALISÉES =====

            // Validation ville
            if (!dto.CityId.HasValue && string.IsNullOrWhiteSpace(dto.CityName))
            {
                return BadRequest(new { Message = "Veuillez sélectionner une ville existante ou fournir le nom d'une nouvelle ville" });
            }

            if (dto.CityId.HasValue && !string.IsNullOrWhiteSpace(dto.CityName))
            {
                return BadRequest(new { Message = "Veuillez choisir entre une ville existante (CityId) ou une nouvelle ville (CityName), pas les deux" });
            }

            // Validation mot de passe admin
            if (!dto.GeneratePassword && string.IsNullOrWhiteSpace(dto.AdminPassword))
            {
                return BadRequest(new { Message = "Le mot de passe est requis si GeneratePassword est false" });
            }

            if (dto.GeneratePassword && !string.IsNullOrWhiteSpace(dto.AdminPassword))
            {
                return BadRequest(new { Message = "Ne fournissez pas de mot de passe si GeneratePassword est true" });
            }

            // ===== RÉCUPRÉRER LE Id du User courant =====
            var currentUserId = User.GetUserId();

            // ===== VÉRIFIER QUE LE PAYS EXISTE =====

            var country = await _db.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CountryId && c.DeletedAt == null);

            if (country == null)
                return NotFound(new { Message = "Pays non trouvé" });

            // ===== GÉRER LA VILLE (Existante ou Nouvelle) =====

            int finalCityId;

            if (dto.CityId.HasValue)
            {
                var existingCity = await _db.Cities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.CityId.Value && c.DeletedAt == null);

                if (existingCity == null)
                    return NotFound(new { Message = "Ville non trouvée" });

                if (existingCity.CountryId != dto.CountryId)
                    return BadRequest(new { Message = "La ville sélectionnée n'appartient pas au pays choisi" });

                finalCityId = dto.CityId.Value;
            }
            else
            {
                var duplicateCity = await _db.Cities
                    .FirstOrDefaultAsync(c =>
                        c.CountryId == dto.CountryId &&
                        c.CityName.ToLower() == dto.CityName!.ToLower() &&
                        c.DeletedAt == null);

                if (duplicateCity != null)
                {
                    finalCityId = duplicateCity.Id;
                }
                else
                {
                    var newCity = new City
                    {
                        CityName = dto.CityName!.Trim(),
                        CountryId = dto.CountryId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = currentUserId,
                    };

                    _db.Cities.Add(newCity);
                    await _db.SaveChangesAsync();
                    finalCityId = newCity.Id;
                }
            }

            // ===== VÉRIFICATIONS D'UNICITÉ ENTREPRISE =====

            if (await _db.Companies.AnyAsync(c => c.Email == dto.CompanyEmail && c.DeletedAt == null))
                return Conflict(new { Message = "Une entreprise avec cet email existe déjà" });

            if (await _db.Companies.AnyAsync(c => c.CnssNumber == dto.CnssNumber && c.DeletedAt == null))
                return Conflict(new { Message = "Une entreprise avec ce numéro CNSS existe déjà" });

            // ===== VÉRIFICATIONS D'UNICITÉ ADMIN =====

            if (await _db.Employees.AnyAsync(e => e.Email == dto.AdminEmail && e.DeletedAt == null))
                return Conflict(new { Message = "Un employé avec cet email existe déjà" });

            if (await _db.Users.AnyAsync(u => u.Email == dto.AdminEmail && u.DeletedAt == null))
                return Conflict(new { Message = "Un utilisateur avec cet email existe déjà" });

            // ===== CRÉER L'ENTREPRISE =====

            var company = new Models.Company.Company
            {
                CompanyName = dto.CompanyName.Trim(),
                Email = dto.CompanyEmail.Trim(),
                PhoneNumber = dto.CompanyPhoneNumber.Trim(),
                CountryPhoneCode = dto.CountryPhoneCode ?? country.CountryPhoneCode,
                CompanyAddress = dto.CompanyAddress.Trim(),
                CityId = finalCityId,
                CountryId = dto.CountryId,
                CnssNumber = dto.CnssNumber.Trim(),
                IsCabinetExpert = dto.IsCabinetExpert,
                IceNumber = dto.IceNumber?.Trim(),
                IfNumber = dto.IfNumber?.Trim(),
                RcNumber = dto.RcNumber?.Trim(),
                RibNumber = dto.RibNumber?.Trim(),
                PatenteNumber = dto.PatenteNumber?.Trim(),
                WebsiteUrl = dto.WebsiteUrl?.Trim(),
                LegalForm = dto.LegalForm?.Trim(),
                FoundingDate = dto.FoundingDate,
                Currency = "MAD",
                PayrollPeriodicity = "Mensuelle",
                FiscalYearStartMonth = 1,
                BusinessSector = dto.BusinessSector?.Trim(),
                PaymentMethod = dto.PaymentMethod?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId
            };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            // Populer la company avec les données par défaut (types de contrat, départements, postes, calendrier, congés)
            await _companyOnboardingService.OnboardAsync(company.Id, currentUserId);

            // ===== RÉCUPÉRER LE STATUT "ACTIVE" =====

            var activeStatus = await _db.Statuses
                .FirstOrDefaultAsync(s => s.Code.ToLower() == "active");

            if (activeStatus == null) return StatusCode(500, new { Message = "Le statut 'Active' est introuvable dans la base de données" });

            // ===== CRÉER L'EMPLOYÉ ADMINISTRATEUR =====

            var adminEmployee = new payzen_backend.Models.Employee.Employee
            {
                FirstName = dto.AdminFirstName.Trim(),
                LastName = dto.AdminLastName.Trim(),
                Email = dto.AdminEmail.Trim(),
                Phone = dto.AdminPhone.Trim(),
                CompanyId = company.Id,
                StatusId = activeStatus.Id,
                CinNumber = "TEMP-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(), // Temporaire
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)), // Temporaire
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId // Système
            };

            _db.Employees.Add(adminEmployee);
            await _db.SaveChangesAsync();

            // ===== GÉNÉRER OU UTILISER LE MOT DE PASSE =====

            string password;

            if (dto.GeneratePassword)
            {
                password = _passwordGenerator.GenerateTemporaryPassword();
            }
            else
            {
                password = dto.AdminPassword!;
            }

            // ===== CRÉER LE COMPTE UTILISATEUR =====

            var username = _passwordGenerator.GenerateUsername(dto.AdminFirstName, dto.AdminLastName);
            var suffix = 1;

            while (await _db.Users.AnyAsync(u => u.Username == username && u.DeletedAt == null))
            {
                username = _passwordGenerator.GenerateUsername(dto.AdminFirstName, dto.AdminLastName, suffix);
                suffix++;
            }

            var adminUser = new Users
            {
                EmployeeId = adminEmployee.Id,
                Username = username,
                Email = dto.AdminEmail.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId // Système
            };

            _db.Users.Add(adminUser);
            await _db.SaveChangesAsync();

            // ===== ASSIGNER LE RÔLE ADMIN =====

            var adminRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.Name.ToLower() == "admin" && r.DeletedAt == null);

            if (adminRole == null)
            {
                // Créer le rôle Admin s'il n'existe pas
                adminRole = new Models.Permissions.Roles
                {
                    Name = "Admin",
                    Description = "Administrateur de l'entreprise",
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = currentUserId
                };
                _db.Roles.Add(adminRole);
                await _db.SaveChangesAsync();
            }

            var userRole = new Models.Permissions.UsersRoles
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId
            };

            _db.UsersRoles.Add(userRole);
            await _db.SaveChangesAsync();

            // ===== PRÉPARER LA RÉPONSE =====

            var createdCompany = await _db.Companies
                .AsNoTracking()
                .Include(c => c.City)
                .Include(c => c.Country)
                .FirstAsync(c => c.Id == company.Id);

            var response = new CompanyCreateResponseDto
            {
                Company = new CompanyReadDto
                {
                    Id = createdCompany.Id,
                    CompanyName = createdCompany.CompanyName,
                    Email = createdCompany.Email,
                    PhoneNumber = createdCompany.PhoneNumber,
                    CountryPhoneCode = createdCompany.CountryPhoneCode,
                    CompanyAddress = createdCompany.CompanyAddress,
                    CityId = createdCompany.CityId,
                    CityName = createdCompany.City?.CityName,
                    CountryId = createdCompany.CountryId,
                    CountryName = createdCompany.Country?.CountryName,
                    CnssNumber = createdCompany.CnssNumber,
                    IsCabinetExpert = createdCompany.IsCabinetExpert,
                    IceNumber = createdCompany.IceNumber,
                    IfNumber = createdCompany.IfNumber,
                    RcNumber = createdCompany.RcNumber,
                    LegalForm = createdCompany.LegalForm,
                    FoundingDate = createdCompany.FoundingDate,
                    BusinessSector = createdCompany.BusinessSector,
                    CreatedAt = createdCompany.CreatedAt.DateTime
                },
                Admin = new AdminAccountDto
                {
                    EmployeeId = adminEmployee.Id,
                    UserId = adminUser.Id,
                    Username = adminUser.Username,
                    Email = adminUser.Email,
                    FirstName = adminEmployee.FirstName,
                    LastName = adminEmployee.LastName,
                    Phone = adminEmployee.Phone,
                    Password = dto.GeneratePassword ? password : null, // Ne retourner le mot de passe que s'il a été généré
                    Message = dto.GeneratePassword
                        ? "Un mot de passe temporaire a été généré. Veuillez le changer lors de la première connexion."
                        : "Compte administrateur créé avec succès."
                }
            };

            return CreatedAtAction(nameof(GetCompanyById), new { id = company.Id }, response);
        }

        /// <summary>
        /// Crée une nouvelle entreprise de la part d'un expert comptable
        /// </summary>
        [HttpPost("create-by-expert")]
        //[HasPermission("CREATE_COMPANY_EXPERT")]
        public async Task<ActionResult<CompanyCreateResponseDto>> CreateCompanyByExpert([FromBody] CompanyCreateByExpertDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Vérifier que le cabinet expert gestionnaire existe et est bien un cabinet
            var managingCompany = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.ManagedByCompanyId && c.DeletedAt == null);

            var currentUserId = User.GetUserId();

            // Vérifier que l'utilisateur courant appartient au cabinet expert gestionnaire
            var currentUser = await _db.Users.FindAsync(currentUserId);

            // Récupérer l'employé lié au user (si présent) en toute sécurité
            var currentEmployee = currentUser != null && currentUser.EmployeeId.HasValue
                ? await _db.Employees.FindAsync(currentUser.EmployeeId.Value)
                : null;

            if (currentUser == null || currentEmployee == null || currentEmployee.CompanyId != dto.ManagedByCompanyId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Message = "L'utilisateur courant n'appartient pas au cabinet gestionnaire ou est invalide"
                });
            }

            if (managingCompany == null)
                return NotFound(new { Message = "Cabinet expert gestionnaire introuvable" });

            if (!managingCompany.IsCabinetExpert)
                return BadRequest(new { Message = "La société spécifiée comme gestionnaire n'est pas un cabinet expert" });

            // Validation ville
            if (!dto.CityId.HasValue && string.IsNullOrWhiteSpace(dto.CityName))
                return BadRequest(new { Message = "Veuillez sélectionner une ville existante ou fournir le nom d'une nouvelle ville" });

            if (dto.CityId.HasValue && !string.IsNullOrWhiteSpace(dto.CityName))
                return BadRequest(new { Message = "Veuillez choisir entre une ville existante (CityId) ou une nouvelle ville (CityName), pas les deux" });

            // Validation mot de passe admin
            if (!dto.GeneratePassword && string.IsNullOrWhiteSpace(dto.AdminPassword))
                return BadRequest(new { Message = "Le mot de passe est requis si GeneratePassword est false" });

            if (dto.GeneratePassword && !string.IsNullOrWhiteSpace(dto.AdminPassword))
                return BadRequest(new { Message = "Ne fournissez pas de mot de passe si GeneratePassword est true" });

            // Vérifier le pays
            var country = await _db.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CountryId && c.DeletedAt == null);

            if (country == null)
                return NotFound(new { Message = "Pays non trouvé" });

            // Gérer la ville (existante ou nouvelle)
            int finalCityId;
            if (dto.CityId.HasValue)
            {
                var existingCity = await _db.Cities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.CityId.Value && c.DeletedAt == null);

                if (existingCity == null)
                    return NotFound(new { Message = "Ville non trouvée" });

                if (existingCity.CountryId != dto.CountryId)
                    return BadRequest(new { Message = "La ville sélectionnée n'appartient pas au pays choisi" });

                finalCityId = dto.CityId.Value;
            }
            else
            {
                var duplicateCity = await _db.Cities
                    .FirstOrDefaultAsync(c =>
                        c.CountryId == dto.CountryId &&
                        c.CityName.ToLower() == dto.CityName!.ToLower() &&
                        c.DeletedAt == null);

                if (duplicateCity != null)
                {
                    finalCityId = duplicateCity.Id;
                }
                else
                {
                    var newCity = new City
                    {
                        CityName = dto.CityName!.Trim(),
                        CountryId = dto.CountryId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = currentUserId,
                    };

                    _db.Cities.Add(newCity);
                    await _db.SaveChangesAsync();
                    finalCityId = newCity.Id;
                }
            }

            // Vérifications d'unicité entreprise & admin (mêmes règles que CreateCompany)
            if (await _db.Companies.AnyAsync(c => c.Email == dto.CompanyEmail && c.DeletedAt == null))
                return Conflict(new { Message = "Une entreprise avec cet email existe déjà" });

            if (await _db.Companies.AnyAsync(c => c.CnssNumber == dto.CnssNumber && c.DeletedAt == null))
                return Conflict(new { Message = "Une entreprise avec ce numéro CNSS existe déjà" });

            if (await _db.Employees.AnyAsync(e => e.Email == dto.AdminEmail && e.DeletedAt == null))
                return Conflict(new { Message = "Un employé avec cet email existe déjà" });

            if (await _db.Users.AnyAsync(u => u.Email == dto.AdminEmail && u.DeletedAt == null))
                return Conflict(new { Message = "Un utilisateur avec cet email existe déjà" });

            // Créer l'entreprise en assignant le gestionnaire (ManagedByCompanyId)
            var company = new Models.Company.Company
            {
                CompanyName = dto.CompanyName.Trim(),
                Email = dto.CompanyEmail.Trim(),
                PhoneNumber = dto.CompanyPhoneNumber.Trim(),
                CountryPhoneCode = dto.CountryPhoneCode ?? country.CountryPhoneCode,
                CompanyAddress = dto.CompanyAddress.Trim(),
                CityId = finalCityId,
                CountryId = dto.CountryId,
                CnssNumber = dto.CnssNumber.Trim(),
                IsCabinetExpert = dto.IsCabinetExpert,
                IceNumber = dto.IceNumber?.Trim(),
                IfNumber = dto.IfNumber?.Trim(),
                RcNumber = dto.RcNumber?.Trim(),
                RibNumber = dto.RibNumber?.Trim(),
                LegalForm = dto.LegalForm?.Trim(),
                PatenteNumber = dto.PatenteNumber?.Trim(),
                WebsiteUrl = dto.WebsiteUrl?.Trim(),
                FoundingDate = dto.FoundingDate,
                Currency = "MAD",
                PayrollPeriodicity = "Mensuelle",
                FiscalYearStartMonth = 1,
                BusinessSector = dto.BusinessSector?.Trim(),
                PaymentMethod = dto.PaymentMethod?.Trim(),
                ManagedByCompanyId = dto.ManagedByCompanyId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId
            };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            // Populer la company avec les données par défaut (types de contrat, départements, postes, calendrier, congés)
            await _companyOnboardingService.OnboardAsync(company.Id, currentUserId);

            // Créer l'employé administrateur
            var activeStatus = await _db.Statuses
                .FirstOrDefaultAsync(s => s.Code.ToLower() == "active");

            var adminEmployee = new payzen_backend.Models.Employee.Employee
            {
                FirstName = dto.AdminFirstName.Trim(),
                LastName = dto.AdminLastName.Trim(),
                Email = dto.AdminEmail.Trim(),
                Phone = dto.AdminPhone.Trim(),
                CompanyId = company.Id,
                StatusId = activeStatus.Id,
                CinNumber = "TEMP-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId
            };

            _db.Employees.Add(adminEmployee);
            await _db.SaveChangesAsync();

            // Mot de passe
            string password = dto.GeneratePassword ? _passwordGenerator.GenerateTemporaryPassword() : dto.AdminPassword!;

            // Générer username unique
            var username = _passwordGenerator.GenerateUsername(dto.AdminFirstName, dto.AdminLastName);
            var suffix = 1;
            while (await _db.Users.AnyAsync(u => u.Username == username && u.DeletedAt == null))
            {
                username = _passwordGenerator.GenerateUsername(dto.AdminFirstName, dto.AdminLastName, suffix);
                suffix++;
            }

            var adminUser = new Users
            {
                EmployeeId = adminEmployee.Id,
                Username = username,
                Email = dto.AdminEmail.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId
            };

            _db.Users.Add(adminUser);
            await _db.SaveChangesAsync();

            // Assigner rôle Admin (même comportement que CreateCompany)
            var adminRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.Name.ToLower() == "admin" && r.DeletedAt == null);

            if (adminRole == null)
            {
                adminRole = new Models.Permissions.Roles
                {
                    Name = "Admin",
                    Description = "Administrateur de l'entreprise",
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = currentUserId
                };
                _db.Roles.Add(adminRole);
                await _db.SaveChangesAsync();
            }

            var userRole = new Models.Permissions.UsersRoles
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId
            };

            _db.UsersRoles.Add(userRole);
            await _db.SaveChangesAsync();

            // Préparer la réponse
            var createdCompany = await _db.Companies
                .AsNoTracking()
                .Include(c => c.City)
                .Include(c => c.Country)
                .FirstAsync(c => c.Id == company.Id);

            var response = new CompanyCreateResponseDto
            {
                Company = new CompanyReadDto
                {
                    Id = createdCompany.Id,
                    CompanyName = createdCompany.CompanyName,
                    Email = createdCompany.Email,
                    PhoneNumber = createdCompany.PhoneNumber,
                    CountryPhoneCode = createdCompany.CountryPhoneCode,
                    CompanyAddress = createdCompany.CompanyAddress,
                    CityId = createdCompany.CityId,
                    CityName = createdCompany.City?.CityName,
                    CountryId = createdCompany.CountryId,
                    CountryName = createdCompany.Country?.CountryName,
                    CnssNumber = createdCompany.CnssNumber,
                    IsCabinetExpert = createdCompany.IsCabinetExpert,
                    IceNumber = createdCompany.IceNumber,
                    IfNumber = createdCompany.IfNumber,
                    RcNumber = createdCompany.RcNumber,
                    LegalForm = createdCompany.LegalForm,
                    PatentNumber = createdCompany.PatenteNumber,
                    WebsiteUrl = createdCompany.WebsiteUrl,
                    FoundingDate = createdCompany.FoundingDate,
                    BusinessSector = createdCompany.BusinessSector,
                    isActive = createdCompany.isActive,
                    CreatedAt = createdCompany.CreatedAt.DateTime
                },
                Admin = new AdminAccountDto
                {
                    EmployeeId = adminEmployee.Id,
                    UserId = adminUser.Id,
                    Username = adminUser.Username,
                    Email = adminUser.Email,
                    FirstName = adminEmployee.FirstName,
                    LastName = adminEmployee.LastName,
                    Phone = adminEmployee.Phone,
                    Password = dto.GeneratePassword ? password : null,
                    Message = dto.GeneratePassword
                        ? "Un mot de passe temporaire a été généré. Veuillez le changer lors de la première connexion."
                        : "Compte administrateur créé avec succès."
                }
            };

            return CreatedAtAction(nameof(GetCompanyById), new { id = company.Id }, response);
        }

        /// <summary>
        /// Return les company manager par un exper
        /// </summary>
        [HttpGet("managedby/{expertCompanyId}")]
        public async Task<ActionResult<IEnumerable<CompanyListDto>>> GetCompaniesManagedBy(int expertCompanyId)
        {
            // Vérifier que le cabinet expert gestionnaire existe
            var managingCompany = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == expertCompanyId && c.DeletedAt == null);

            if (managingCompany == null)
                return NotFound(new { Message = "Cabinet gestionnaire introuvable" });

            if (!managingCompany.IsCabinetExpert)
                return BadRequest(new { Message = "La société spécifiée n'est pas un cabinet expert" });

            // Récupérer les entreprises dont ManagedByCompanyId == expertCompanyId
            var companies = await _db.Companies
                .AsNoTracking()
                .Where(c => c.DeletedAt == null && c.ManagedByCompanyId == expertCompanyId)
                .Include(c => c.City)
                .Include(c => c.Country)
                .OrderBy(c => c.CompanyName)
                .Select(c => new CompanyListDto
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    IsCabinetExpert = c.IsCabinetExpert,
                    Email = c.Email,
                    CountryPhoneCode = c.Country != null ? c.Country.CountryPhoneCode : null,
                    CityName = c.City != null ? c.City.CityName : null,
                    CountryName = c.Country != null ? c.Country.CountryName : null,
                    CnssNumber = c.CnssNumber,
                    RibNumber = c.RibNumber,
                    WebsiteUrl = c.WebsiteUrl,
                    PatenteNumber = c.PatenteNumber,
                    isActive = c.isActive,
                    CreatedAt = c.CreatedAt.DateTime
                })
                .ToListAsync();

            return Ok(companies);
        }

        /// <summary>
        /// Mise à jour partielle d'une entreprise (PATCH /api/companies/{id})
        /// Supporte : modification des champs simples, changement/création de ville, vérifications d'unicité.
        /// </summary>
        [HttpPatch("{id}")]
        //[HasPermission("EDIT_COMPANY")]
        public async Task<ActionResult<CompanyReadDto>> PatchCompany(int id, [FromBody] CompanyUpdateDto dto)
        {
            if (dto == null)
                return BadRequest(new { Message = "Données de mise à jour requises" });


            var currentUserId = User.GetUserId();

            var company = await _db.Companies
                .Include(c => c.City)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (company == null)
                return NotFound(new { Message = "Entreprise non trouvée" });

            if (!string.IsNullOrWhiteSpace(dto.email) && dto.email.Trim() != company.Email)
            {
                var newEmail = dto.email.Trim();

                var exists = await _db.Companies.AnyAsync(c =>
                    c.Email == newEmail &&
                    c.DeletedAt == null &&
                    c.Id != id
                );

                if (exists)
                    return Conflict(new { Message = "Une autre entreprise utilise déjà cet email" });

                var oldEmail = company.Email;
                company.Email = newEmail;

                await _companyEventLogService.LogEventAsync(
                    company.Id,
                    "Email_Changed",
                    oldEmail,
                    null,
                    newEmail,
                    null,
                    currentUserId
                );
            }

            if (!string.IsNullOrWhiteSpace(dto.CompanyName) && dto.CompanyName.Trim() != company.CompanyName)
            {
                var nameExists = await _db.Companies
                    .AnyAsync(c =>
                              c.CompanyName.ToLower() == dto.CompanyName.Trim().ToLower() &&
                              c.DeletedAt == null &&
                              c.Id != id);

                if (nameExists)
                    return Conflict(new { Message = "Une autre entreprise utilise déjà ce nom" });

                await _companyEventLogService.LogEventAsync(
                    company.Id,
                    "CompanyName_Changed",
                    company.CompanyName,
                    null,
                    dto.CompanyName,
                    null,
                    currentUserId
                );
                company.CompanyName = dto.CompanyName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.phoneNumber) && dto.phoneNumber.Trim() != company.PhoneNumber)
            {
                // Vérification de l'unicité du numéro de téléphone
                var phoneExists = await _db.Companies
                    .AnyAsync(c =>
                    c.PhoneNumber == dto.phoneNumber.Trim() &&
                    c.DeletedAt == null &&
                    c.Id != id
                    );

                if (phoneExists)
                    return Conflict(new { Message = "Une autre entreprise utilise déjà ce numéro de téléphone" });

                await _companyEventLogService.LogEventAsync(
                    company.Id,
                    "Phone_Changed",
                    company.PhoneNumber,
                    null,
                    dto.phoneNumber,
                    null,
                    currentUserId
                );

                company.PhoneNumber = dto.phoneNumber.Trim();

                Console.WriteLine($"Company.PhoneNumber set to: '{company.PhoneNumber}'");
            }

            if (!string.IsNullOrWhiteSpace(dto.CompanyAddress) && dto.CompanyAddress.Trim() != company.CompanyAddress)
            {
                await _companyEventLogService.LogEventAsync(
                    company.Id,
                    "Address_Changed",
                    company.CompanyAddress,
                    null,
                    dto.CompanyAddress,
                    null,
                    currentUserId
                );

                company.CompanyAddress = dto.CompanyAddress.Trim();
            }

            if (dto.IsCabinetExpert.HasValue && dto.IsCabinetExpert.Value != company.IsCabinetExpert)
            {
                await _companyEventLogService.LogSimpleEventAsync(
                    company.Id,
                    "IsCabinetExpert_Changed",
                    company.IsCabinetExpert.ToString(),
                    dto.IsCabinetExpert.Value.ToString(),
                    currentUserId
                );

                company.IsCabinetExpert = dto.IsCabinetExpert.Value;
            }

            // --- Gérer le pays si envoyé par nom ou id ---
            if (!string.IsNullOrWhiteSpace(dto.CountryName) || dto.CountryId.HasValue)
            {
                if (dto.CountryId.HasValue)
                {
                    if (company.CountryId != dto.CountryId.Value)
                    {
                        var existingCountry = await _db.Countries
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Id == dto.CountryId.Value && c.DeletedAt == null);

                        if (existingCountry == null)
                            return NotFound(new { Message = "Pays non trouvé" });

                        await _companyEventLogService.LogRelationEventAsync(
                            company.Id,
                            "Country_Changed",
                            company.CountryId,
                            company.Country?.CountryName,
                            existingCountry.Id,
                            existingCountry.CountryName,
                            currentUserId
                        );

                        company.CountryId = existingCountry.Id;
                        company.CountryPhoneCode = existingCountry.CountryPhoneCode;
                        Console.WriteLine($"Country updated by id -> {existingCountry.CountryName}");
                    }
                }
                else
                {
                    var countryNameTrim = dto.CountryName!.Trim();
                    var existingCountry = await _db.Countries
                        .FirstOrDefaultAsync(c => c.CountryName.ToLower() == countryNameTrim.ToLower() && c.DeletedAt == null);

                    if (existingCountry != null)
                    {
                        await _companyEventLogService.LogRelationEventAsync(
                            company.Id,
                            "Country_Changed",
                            company.CountryId,
                            company.Country?.CountryName,
                            existingCountry.Id,
                            existingCountry.CountryName,
                            currentUserId
                        );

                        company.CountryId = existingCountry.Id;
                        company.CountryPhoneCode = existingCountry.CountryPhoneCode;
                        Console.WriteLine($"Country set to existing -> {existingCountry.CountryName}");
                    }
                    else
                    {
                        // Création d'un pays minimal si introuvable (utiliser indicatif fourni ou fallback)
                        var phoneCode = !string.IsNullOrWhiteSpace(dto.CountryPhoneCode)
                            ? dto.CountryPhoneCode!.Trim()
                            : company.CountryPhoneCode ?? "+000";

                        var code = countryNameTrim.Length >= 3
                            ? new string(countryNameTrim.Where(char.IsLetter).Take(3).ToArray()).ToUpper()
                            : countryNameTrim.ToUpper();

                        if (string.IsNullOrWhiteSpace(code))
                            code = "UNK";

                        var newCountry = new Models.Referentiel.Country
                        {
                            CountryName = countryNameTrim,
                            CountryNameAr = null,
                            CountryCode = code,
                            CountryPhoneCode = phoneCode,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreatedBy = currentUserId
                        };

                        _db.Countries.Add(newCountry);
                        await _db.SaveChangesAsync();

                        await _companyEventLogService.LogRelationEventAsync(
                            company.Id,
                            "Country_Changed",
                            company.CountryId,
                            company.Country?.CountryName,
                            newCountry.Id,
                            newCountry.CountryName,
                            currentUserId
                        );

                        company.CountryId = newCountry.Id;
                        company.CountryPhoneCode = newCountry.CountryPhoneCode;
                        Console.WriteLine($"Country created -> {newCountry.CountryName}");
                    }
                }
            }

            // --- Gestion de la ville (par Id ou par Nom) ---
            if (!string.IsNullOrWhiteSpace(dto.CityName) || dto.CityId.HasValue)
            {
                // Country obligatoire pour la ville
                if (company.CountryId <= 0)
                    return BadRequest(new { Message = "CountryId requis pour créer/rechercher une ville." });

                int newCityId;
                string newCityName;

                // ==========================
                // Cas 1 : CityId fourni
                // ==========================
                if (dto.CityId.HasValue)
                {
                    // Aucun changement réel
                    if (company.CityId == dto.CityId.Value)
                        goto END_CITY_UPDATE;

                    var existingCity = await _db.Cities
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c =>
                            c.Id == dto.CityId.Value &&
                            c.DeletedAt == null);

                    if (existingCity == null)
                        return NotFound(new { Message = "Ville non trouvée" });

                    if (existingCity.CountryId != company.CountryId)
                        return BadRequest(new { Message = "La ville choisie n'appartient pas au pays courant de l'entreprise" });

                    newCityId = existingCity.Id;
                    newCityName = existingCity.CityName;
                }
                // ==========================
                // Cas 2 : CityName fourni
                // ==========================
                else
                {
                    var cityNameTrim = dto.CityName!.Trim();

                    var existingCity = await _db.Cities.FirstOrDefaultAsync(c =>
                        c.CityName.ToLower() == cityNameTrim.ToLower() &&
                        c.CountryId == company.CountryId &&
                        c.DeletedAt == null);

                    if (existingCity != null)
                    {
                        newCityId = existingCity.Id;
                        newCityName = existingCity.CityName;
                    }
                    else
                    {
                        var newCity = new Models.Referentiel.City
                        {
                            CityName = cityNameTrim,
                            CountryId = company.CountryId,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreatedBy = currentUserId
                        };

                        _db.Cities.Add(newCity);
                        await _db.SaveChangesAsync();

                        newCityId = newCity.Id;
                        newCityName = newCity.CityName;
                    }
                }

                // ==========================
                // LOG + UPDATE UNIQUEMENT SI CHANGEMENT RÉEL
                // ==========================
                if (company.CityId != newCityId)
                {
                    await _companyEventLogService.LogRelationEventAsync(
                        company.Id,
                        CompanyEventLogService.EventNames.CityChanged,
                        company.CityId,
                        company.City?.CityName,
                        newCityId,
                        newCityName,
                        currentUserId
                    );

                    company.CityId = newCityId;
                }

            END_CITY_UPDATE:;
            }


            // --- mise à jour CNSS ----
            if (!string.IsNullOrWhiteSpace(dto.CnssNumber))
            {
                var cnssExists = await _db.Companies
                    .AnyAsync(c =>
                    c.CnssNumber == dto.CnssNumber.Trim() &&
                    c.DeletedAt == null &&
                    c.Id != id
                    );

                if (cnssExists)
                    return Conflict(new { Message = "Une autre entreprise utilise déjà ce numéro CNSS" });

                var cnssTrim = dto.CnssNumber!.Trim();

                if (cnssTrim != company.CnssNumber)
                {
                    await _companyEventLogService.LogEventAsync(
                        company.Id,
                        "Cnss_Changed",
                        company.CnssNumber,
                        null,
                        cnssTrim,
                        null,
                        currentUserId
                    );

                    company.CnssNumber = cnssTrim;
                }
            }

            // ---- mise à jour ice ----
            if (!string.IsNullOrWhiteSpace(dto.IceNumber))
            {
                var iceExists = await _db.Companies
                    .AnyAsync(c =>
                    c.IceNumber == dto.IceNumber.Trim() &&
                    c.DeletedAt == null &&
                    c.Id != id
                    );

                if (iceExists)
                    return Conflict(new { Message = "Une autre entreprise utilise déjà ce numéro ICE" });

                var iceTrim = dto.IceNumber!.Trim();

                if (iceTrim != company.IceNumber)
                {
                    await _companyEventLogService.LogEventAsync(
                        company.Id,
                        "Ice_Changed",
                        company.IceNumber,
                        null,
                        iceTrim,
                        null,
                        currentUserId
                    );

                    company.IceNumber = iceTrim;
                }
            }

            // ----- Mise à Jour IfNumber
            if (!string.IsNullOrWhiteSpace(dto.IfNumber))
            {
                var ifExists = await _db.Companies
                    .AnyAsync(c =>
                    c.IfNumber == dto.IfNumber.Trim() &&
                    c.DeletedAt == null &&
                    c.Id != id
                    );
                if (ifExists)
                    return Conflict(new { Message = "Une autre entreprise utilise déjà ce numéro IF" });
                var ifTrim = dto.IfNumber!.Trim();
                if (ifTrim != company.IfNumber)
                {
                    await _companyEventLogService.LogEventAsync(
                        company.Id,
                        "If_Changed",
                        company.IfNumber,
                        null,
                        ifTrim,
                        null,
                        currentUserId
                    );

                    company.IfNumber = ifTrim;
                }
            }

            // mise à jour RC
            if (!string.IsNullOrWhiteSpace(dto.RcNumber))
            {
                var rcExists = await _db.Companies
                    .AnyAsync(c =>
                    c.RcNumber == dto.RcNumber.Trim() &&
                    c.DeletedAt == null &&
                    c.Id != id
                    );

                if (rcExists)
                    return Conflict(new { Message = "Une autre entreprise utilise déjà ce numéro RC" });

                var rcTrim = dto.RcNumber!.Trim();

                if (rcTrim != company.RcNumber)
                {
                    await _companyEventLogService.LogEventAsync(
                        company.Id,
                        "Rc_Changed",
                        company.RcNumber,
                        null,
                        rcTrim,
                        null,
                        currentUserId
                    );

                    company.RcNumber = rcTrim;
                }
            }
            // Mise à jour RIB
            if(!string.IsNullOrWhiteSpace(dto.RibNumber))
            {
                var ribTrim = dto.RibNumber!.Trim();
                if (ribTrim != company.RibNumber)
                {
                    await _companyEventLogService.LogEventAsync(
                        company.Id,
                        "Rib_Changed",
                        company.RibNumber,
                        null,
                        ribTrim,
                        null,
                        currentUserId
                    );
                    company.RibNumber = ribTrim;
                }
            }

            // Mise à jour Pattente
            if (!string.IsNullOrWhiteSpace(dto.PatenteNumber))
            {
                var patentTrim = dto.PatenteNumber!.Trim();
                if (patentTrim != company.PatenteNumber)
                {
                    await _companyEventLogService.LogEventAsync(
                        company.Id,
                        "Patent_Changed",
                        company.PatenteNumber,
                        null,
                        patentTrim,
                        null,
                        currentUserId
                    );
                    company.PatenteNumber = patentTrim;
                }
            }
            // Mise à Jour website
            if(!string.IsNullOrWhiteSpace(dto.WebsiteUrl))
            {
                var websiteTrim = dto.WebsiteUrl!.Trim();
                if (websiteTrim != company.WebsiteUrl)
                {
                    await _companyEventLogService.LogEventAsync(
                        company.Id,
                        "WebsiteUrl_Changed",
                        company.WebsiteUrl,
                        null,
                        websiteTrim,
                        null,
                        currentUserId
                    );
                    company.WebsiteUrl = websiteTrim;
                }
            }

            // Mise à jour form juridique
            if (!string.IsNullOrEmpty(dto.LegalForm))
            {
                var legalFormTrim = dto.LegalForm!.Trim();
                if (legalFormTrim != company.LegalForm)
                {
                    await _companyEventLogService.LogEventAsync(
                        company.Id,
                        "LegalForm_Changed",
                        company.LegalForm,
                        null,
                        legalFormTrim,
                        null,
                        currentUserId
                    );

                    company.LegalForm = legalFormTrim;
                }
            }

            // Mise à jour date de fondation
            if (dto.FoundingDate.HasValue && dto.FoundingDate.Value != company.FoundingDate)
            {
                var oldVal = company.FoundingDate?.ToString("o");
                var newVal = dto.FoundingDate.Value.ToString("o");

                await _companyEventLogService.LogEventAsync(
                    company.Id,
                    "FoundingDate_Changed",
                    oldVal,
                    null,
                    newVal,
                    null,
                    currentUserId
                );

                company.FoundingDate = dto.FoundingDate.Value;
            }

            // Désactiver / Réactiver une company = uniquement changer isActive (pas de DeletedAt)
            if (dto.isActive.HasValue && dto.isActive.Value != company.isActive)
            {
                var oldStatus = company.isActive ? "Active" : "Inactive";
                var newStatus = dto.isActive.Value ? "Active" : "Inactive";

                await _companyEventLogService.LogSimpleEventAsync(
                    company.Id,
                    "Status_Changed",
                    oldStatus,
                    newStatus,
                    currentUserId
                );

                company.isActive = dto.isActive.Value;
            }

            // ===== Audit =====
            company.ModifiedAt = DateTimeOffset.UtcNow;
            company.ModifiedBy = currentUserId;

            var changes = _db.Entry(company).Properties
                .Where(p => p.IsModified)
                .Select(p => $"{p.Metadata.Name} => {p.CurrentValue}")
                .ToList();

            // Log before saving
            Console.WriteLine("Changes to save:");
            foreach (var change in changes)
                Console.WriteLine(change);

            await _db.SaveChangesAsync();

            Console.WriteLine("SaveChanges completed");

            var updated = await _db.Companies
                .AsNoTracking()
                .Include(c => c.City)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Id == company.Id);

            var result = new CompanyReadDto
            {
                Id = updated.Id,
                CompanyName = updated.CompanyName,
                Email = updated.Email,
                PhoneNumber = updated.PhoneNumber,
                CountryPhoneCode = updated.CountryPhoneCode,
                CompanyAddress = updated.CompanyAddress,
                CityId = updated.CityId,
                CityName = updated.City?.CityName,
                CountryId = updated.CountryId,
                CountryName = updated.Country?.CountryName,
                CnssNumber = updated.CnssNumber,
                IsCabinetExpert = updated.IsCabinetExpert,
                IceNumber = updated.IceNumber,
                IfNumber = updated.IfNumber,
                RcNumber = updated.RcNumber,
                LegalForm = updated.LegalForm,
                WebsiteUrl = updated.WebsiteUrl,
                PatentNumber = updated.PatenteNumber,
                RibNumber = updated.RibNumber,
                FoundingDate = updated.FoundingDate,
                BusinessSector = updated.BusinessSector,
                isActive = updated.isActive,
                CreatedAt = updated.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Récupère l'historique des modifications d'une entreprise (company) uniquement (sans les events employee).
        /// </summary>
        /// <param name="companyId"></param>
        /// <returns></returns>
        [HttpGet("{companyId}/history")]
        //[HasPermission("VIEW_COMPANY_HISTORY")
        public async Task<IActionResult> GetCompanyHistory(int companyId)
        {
            // Vérifier existence de l'entreprise
            var exists = await _db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId && c.DeletedAt == null);
            if (!exists)
                return NotFound(new { Message = "Entreprise non trouvée" });

            // Récupérer uniquement les events company
            var companyEvents = await _db.CompanyEventLogs
                .AsNoTracking()
                .Where(e => e.companyId == companyId)
                .Select(e => new
                {
                    EventName = e.eventName,
                    e.oldValue,
                    e.oldValueId,
                    e.newValue,
                    e.newValueId,
                    CreatedAt = e.createdAt,
                    CreatedBy = e.createdBy,
                    EmployeeId = e.employeeId,
                    Source = "company"
                })
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            // Si aucun event, retourner liste vide
            if (!companyEvents.Any())
                return Ok(new List<CompanyHistoryDto>());

            // Récupérer les créateurs (users) et leurs rôles pour ces events
            var creatorIds = companyEvents.Select(e => e.CreatedBy).Where(id => id != 0).Distinct().ToList();

            var users = await _db.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .Where(u => creatorIds.Contains(u.Id))
                .ToListAsync();

            var usersRoles = await _db.UsersRoles
                .AsNoTracking()
                .Include(ur => ur.Role)
                .Where(ur => creatorIds.Contains(ur.UserId))
                .ToListAsync();

            var userRoleMap = usersRoles
                .GroupBy(ur => ur.UserId)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.Role?.Name).Where(n => n != null)));

            var userMap = users.ToDictionary(u => u.Id, u => u);

            // Mapper vers CompanyHistoryDto (qui contient : qui a fait quoi et quand)
            var history = companyEvents.Select(e =>
            {
                // qui
                string? name = null;
                string? role = null;
                if (userMap.TryGetValue(e.CreatedBy, out var u))
                {
                    name = u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}" : u.Email;
                    userRoleMap.TryGetValue(u.Id, out role);
                }

                // quoi (titre) = eventName ; description = old -> new si présent
                var title = e.EventName ?? "Événement";
                var description = BuildDescription(e.EventName, e.oldValue, e.newValue);

                return new CompanyHistoryDto
                {
                    Type = e.Source,
                    Title = title,
                    Date = e.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = description,
                    Details = new Dictionary<string, object?>
                    {
                        ["oldValue"] = e.oldValue,
                        ["oldValueId"] = e.oldValueId,
                        ["newValue"] = e.newValue,
                        ["newValueId"] = e.newValueId,
                        ["employeeId"] = e.EmployeeId,
                        ["source"] = e.Source
                    },
                    ModifiedBy = name == null && role == null ? null : new ModifiedByDto { Name = name, Role = role },
                    Timestamp = e.CreatedAt.ToString("o")
                };
            })
            .ToList();

            return Ok(history);
        }
        // Helper local dans le contrôleur
        private static string BuildDescription(string? eventName, string? oldValue, string? newValue)
        {
            if (!string.IsNullOrEmpty(oldValue) || !string.IsNullOrEmpty(newValue))
            {
                var oldVal = string.IsNullOrEmpty(oldValue) ? "<vide>" : oldValue;
                var newVal = string.IsNullOrEmpty(newValue) ? "<vide>" : newValue;
                return $"{eventName ?? "Événement"} : {oldVal} → {newVal}";
            }

            return eventName ?? "Événement";
        }
    }
}