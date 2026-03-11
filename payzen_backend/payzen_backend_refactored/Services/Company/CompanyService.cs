using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Services;
using payzen_backend.Models.Company.Dtos;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Users;
using payzen_backend.Models.Permissions;
using payzen_backend.Services.Company.Interfaces;
using payzen_backend.Services.Company.Onboarding;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace payzen_backend.Services.Company
{
    public class CompanyService : ICompanyService
    {
        private readonly AppDbContext _db;
        private readonly PasswordGeneratorService _passwordGenerator;
        private readonly CompanyEventLogService _companyEventLogService;
        private readonly EmployeeEventLogService _employeeEventLogService;
        private readonly ICompanyOnboardingService _companyOnboardingService;

        public CompanyService(
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

        public async Task<CompanyCreateResponseDto> CreateCompanyAsync(CompanyCreateDto dto, int currentUserId)
        {
            // Input validation moved from controller: throw ServiceException with appropriate HTTP status
            if (dto == null)
                throw new ServiceException(400, "Données de création manquantes");

            // City validation (either CityId or CityName)
            if (!dto.CityId.HasValue && string.IsNullOrWhiteSpace(dto.CityName))
                throw new ServiceException(400, "Veuillez sélectionner une ville existante ou fournir le nom d'une nouvelle ville");

            if (dto.CityId.HasValue && !string.IsNullOrWhiteSpace(dto.CityName))
                throw new ServiceException(400, "Veuillez choisir entre une ville existante (CityId) ou une nouvelle ville (CityName), pas les deux");

            // Password validation
            if (!dto.GeneratePassword && string.IsNullOrWhiteSpace(dto.AdminPassword))
                throw new ServiceException(400, "Le mot de passe est requis si GeneratePassword est false");

            if (dto.GeneratePassword && !string.IsNullOrWhiteSpace(dto.AdminPassword))
                throw new ServiceException(400, "Ne fournissez pas de mot de passe si GeneratePassword est true");

            // Vérifier le pays
            var country = await _db.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CountryId && c.DeletedAt == null);

            if (country == null)
                throw new ServiceException(404, "Pays non trouvé");

            // Gérer la ville
            int finalCityId;
            if (dto.CityId.HasValue)
            {
                var existingCity = await _db.Cities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.CityId.Value && c.DeletedAt == null);

                if (existingCity == null)
                    throw new ServiceException(404, "Ville non trouvée");

                if (existingCity.CountryId != dto.CountryId)
                    throw new ServiceException(400, "La ville sélectionnée n'appartient pas au pays choisi");

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
                    var newCity = new payzen_backend.Models.Referentiel.City
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

            // Unicité entreprise
            if (await _db.Companies.AnyAsync(c => c.Email == dto.CompanyEmail && c.DeletedAt == null))
                throw new ServiceException(409, "Une entreprise avec cet email existe déjà");

            if (await _db.Companies.AnyAsync(c => c.CnssNumber == dto.CnssNumber && c.DeletedAt == null))
                throw new ServiceException(409, "Une entreprise avec ce numéro CNSS existe déjà");

            // Unicité admin
            if (await _db.Employees.AnyAsync(e => e.Email == dto.AdminEmail && e.DeletedAt == null))
                throw new ServiceException(409, "Un employé avec cet email existe déjà");

            if (await _db.Users.AnyAsync(u => u.Email == dto.AdminEmail && u.DeletedAt == null))
                throw new ServiceException(409, "Un utilisateur avec cet email existe déjà");

            // Créer l'entreprise
            var company = new payzen_backend.Models.Company.Company
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

            // Onboard company
            await _companyOnboardingService.OnboardAsync(company.Id, currentUserId);

            // Récupérer statut active
            var activeStatus = await _db.Statuses.FirstOrDefaultAsync(s => s.Code.ToLower() == "active");
            if (activeStatus == null)
                throw new ServiceException(500, "Le statut 'Active' est introuvable dans la base de données");

            // Créer employé admin
            var adminEmployee = new Employee
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

            // Assigner le rôle Admin
            var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "admin" && r.DeletedAt == null);
            if (adminRole == null)
            {
                adminRole = new Roles
                {
                    Name = "Admin",
                    Description = "Administrateur de l'entreprise",
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = currentUserId
                };
                _db.Roles.Add(adminRole);
                await _db.SaveChangesAsync();
            }

            var userRole = new UsersRoles
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
                    Password = dto.GeneratePassword ? password : null,
                    Message = dto.GeneratePassword
                        ? "Un mot de passe temporaire a été généré. Veuillez le changer lors de la première connexion."
                        : "Compte administrateur créé avec succès."
                }
            };

            return response;
        }

        public async Task<IEnumerable<CompanyListDto>> GetAllCompaniesAsync()
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

            return companies;
        }

        public async Task<CompanyListDto?> GetCompanyByIdAsync(int id)
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
                    SignatoryName = c.SignatoryName,
                    SignatoryTitle = c.SignatoryTitle,
                    CreatedAt = c.CreatedAt.DateTime
                })
                .FirstOrDefaultAsync();

            return company;
        }

        public async Task<IEnumerable<CompanyListDto>> GetCompaniesByCityAsync(int cityId)
        {
            var cityExists = await _db.Cities.AnyAsync(c => c.Id == cityId && c.DeletedAt == null);
            if (!cityExists)
                throw new ServiceException(404, "Ville non trouvée");

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

            return companies;
        }

        public async Task<IEnumerable<CompanyListDto>> GetCompaniesByCountryAsync(int countryId)
        {
            var countryExists = await _db.Countries.AnyAsync(c => c.Id == countryId && c.DeletedAt == null);
            if (!countryExists)
                throw new ServiceException(404, "Pays non trouvé");

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

            return companies;
        }

        public async Task<IEnumerable<CompanyListDto>> SearchCompaniesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ServiceException(400, "Le terme de recherche est requis");

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

            return companies;
        }

        public async Task<CompanyFormDataDto> GetFormDataAsync()
        {
            var formData = new CompanyFormDataDto();

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

            return formData;
        }

        public async Task<CompanyCreateResponseDto> CreateCompanyByExpertAsync(CompanyCreateByExpertDto dto, int currentUserId)
        {
            if (dto == null)
                throw new ServiceException(400, "Données de création manquantes");

            // Vérifier que le cabinet expert gestionnaire existe et est bien un cabinet
            var managingCompany = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.ManagedByCompanyId && c.DeletedAt == null);

            // Vérifier l'utilisateur courant
            var currentUser = await _db.Users.FindAsync(currentUserId);
            var currentEmployee = currentUser != null && currentUser.EmployeeId.HasValue
                ? await _db.Employees.FindAsync(currentUser.EmployeeId.Value)
                : null;

            if (currentUser == null || currentEmployee == null || currentEmployee.CompanyId != dto.ManagedByCompanyId)
                throw new ServiceException(403, "L'utilisateur courant n'appartient pas au cabinet gestionnaire ou est invalide");

            if (managingCompany == null)
                throw new ServiceException(404, "Cabinet expert gestionnaire introuvable");

            if (!managingCompany.IsCabinetExpert)
                throw new ServiceException(400, "La société spécifiée comme gestionnaire n'est pas un cabinet expert");

            // Validation ville
            if (!dto.CityId.HasValue && string.IsNullOrWhiteSpace(dto.CityName))
                throw new ServiceException(400, "Veuillez sélectionner une ville existante ou fournir le nom d'une nouvelle ville");

            if (dto.CityId.HasValue && !string.IsNullOrWhiteSpace(dto.CityName))
                throw new ServiceException(400, "Veuillez choisir entre une ville existante (CityId) ou une nouvelle ville (CityName), pas les deux");

            // Validation mot de passe admin
            if (!dto.GeneratePassword && string.IsNullOrWhiteSpace(dto.AdminPassword))
                throw new ServiceException(400, "Le mot de passe est requis si GeneratePassword est false");

            if (dto.GeneratePassword && !string.IsNullOrWhiteSpace(dto.AdminPassword))
                throw new ServiceException(400, "Ne fournissez pas de mot de passe si GeneratePassword est true");

            // Vérifier le pays
            var country = await _db.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CountryId && c.DeletedAt == null);

            if (country == null)
                throw new ServiceException(404, "Pays non trouvé");

            // Gérer la ville (existante ou nouvelle)
            int finalCityId;
            if (dto.CityId.HasValue)
            {
                var existingCity = await _db.Cities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.CityId.Value && c.DeletedAt == null);

                if (existingCity == null)
                    throw new ServiceException(404, "Ville non trouvée");

                if (existingCity.CountryId != dto.CountryId)
                    throw new ServiceException(400, "La ville sélectionnée n'appartient pas au pays choisi");

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
                    var newCity = new payzen_backend.Models.Referentiel.City
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

            // Unicité entreprise
            if (await _db.Companies.AnyAsync(c => c.Email == dto.CompanyEmail && c.DeletedAt == null))
                throw new ServiceException(409, "Une entreprise avec cet email existe déjà");

            if (await _db.Companies.AnyAsync(c => c.CnssNumber == dto.CnssNumber && c.DeletedAt == null))
                throw new ServiceException(409, "Une entreprise avec ce numéro CNSS existe déjà");

            // Unicité admin
            if (await _db.Employees.AnyAsync(e => e.Email == dto.AdminEmail && e.DeletedAt == null))
                throw new ServiceException(409, "Un employé avec cet email existe déjà");

            if (await _db.Users.AnyAsync(u => u.Email == dto.AdminEmail && u.DeletedAt == null))
                throw new ServiceException(409, "Un utilisateur avec cet email existe déjà");

            // Créer l'entreprise
            var company = new payzen_backend.Models.Company.Company
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
                ManagedByCompanyId = dto.ManagedByCompanyId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId
            };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            // Onboard company
            await _companyOnboardingService.OnboardAsync(company.Id, currentUserId);

            // Récupérer statut active
            var activeStatus = await _db.Statuses.FirstOrDefaultAsync(s => s.Code.ToLower() == "active");
            if (activeStatus == null)
                throw new ServiceException(500, "Le statut 'Active' est introuvable dans la base de données");

            // Créer employé admin
            var adminEmployee = new Employee
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

            // Assigner le rôle Admin
            var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "admin" && r.DeletedAt == null);
            if (adminRole == null)
            {
                adminRole = new Roles
                {
                    Name = "Admin",
                    Description = "Administrateur de l'entreprise",
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = currentUserId
                };
                _db.Roles.Add(adminRole);
                await _db.SaveChangesAsync();
            }

            var userRole = new UsersRoles
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
                    Password = dto.GeneratePassword ? password : null,
                    Message = dto.GeneratePassword
                        ? "Un mot de passe temporaire a été généré. Veuillez le changer lors de la première connexion."
                        : "Compte administrateur créé avec succès."
                }
            };

            return response;
        }

        public async Task<IEnumerable<CompanyListDto>> GetCompaniesManagedByAsync(int expertCompanyId)
        {
            var managingCompany = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == expertCompanyId && c.DeletedAt == null);

            if (managingCompany == null)
                throw new ServiceException(404, "Cabinet gestionnaire introuvable");

            if (!managingCompany.IsCabinetExpert)
                throw new ServiceException(400, "La société spécifiée n'est pas un cabinet expert");

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

            return companies;
        }

        public async Task<CompanyReadDto> PatchCompanyAsync(int id, CompanyUpdateDto dto, int currentUserId)
        {
            if (dto == null)
                throw new ServiceException(400, "Données de mise à jour requises");

            var company = await _db.Companies
                .Include(c => c.City)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (company == null)
                throw new ServiceException(404, "Entreprise non trouvée");

            // Email
            if (!string.IsNullOrWhiteSpace(dto.email) && dto.email.Trim() != company.Email)
            {
                var newEmail = dto.email.Trim();

                var exists = await _db.Companies.AnyAsync(c =>
                    c.Email == newEmail &&
                    c.DeletedAt == null &&
                    c.Id != id);

                if (exists)
                    throw new ServiceException(409, "Une autre entreprise utilise déjà cet email");

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

            // CompanyName
            if (!string.IsNullOrWhiteSpace(dto.CompanyName) && dto.CompanyName.Trim() != company.CompanyName)
            {
                var nameExists = await _db.Companies
                    .AnyAsync(c =>
                              c.CompanyName.ToLower() == dto.CompanyName.Trim().ToLower() &&
                              c.DeletedAt == null &&
                              c.Id != id);

                if (nameExists)
                    throw new ServiceException(409, "Une autre entreprise utilise déjà ce nom");

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

            // Phone
            if (!string.IsNullOrWhiteSpace(dto.phoneNumber) && dto.phoneNumber.Trim() != company.PhoneNumber)
            {
                var phoneExists = await _db.Companies
                    .AnyAsync(c =>
                    c.PhoneNumber == dto.phoneNumber.Trim() &&
                    c.DeletedAt == null &&
                    c.Id != id);

                if (phoneExists)
                    throw new ServiceException(409, "Une autre entreprise utilise déjà ce numéro de téléphone");

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
            }

            // Address
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

            // Country handling
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
                            throw new ServiceException(404, "Pays non trouvé");

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
                    }
                    else
                    {
                        var phoneCode = !string.IsNullOrWhiteSpace(dto.CountryPhoneCode)
                            ? dto.CountryPhoneCode!.Trim()
                            : company.CountryPhoneCode ?? "+000";

                        var code = countryNameTrim.Length >= 3
                            ? new string(countryNameTrim.Where(char.IsLetter).Take(3).ToArray()).ToUpper()
                            : countryNameTrim.ToUpper();

                        if (string.IsNullOrWhiteSpace(code))
                            code = "UNK";

                        var newCountry = new payzen_backend.Models.Referentiel.Country
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
                    }
                }
            }

            // City handling (similar to controller)
            if (!string.IsNullOrWhiteSpace(dto.CityName) || dto.CityId.HasValue)
            {
                if (company.CountryId <= 0)
                    throw new ServiceException(400, "CountryId requis pour créer/rechercher une ville.");

                int newCityId;
                string newCityName;

                if (dto.CityId.HasValue)
                {
                    if (company.CityId == dto.CityId.Value)
                        goto END_CITY_UPDATE;

                    var existingCity = await _db.Cities
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == dto.CityId.Value && c.DeletedAt == null);

                    if (existingCity == null)
                        throw new ServiceException(404, "Ville non trouvée");

                    if (existingCity.CountryId != company.CountryId)
                        throw new ServiceException(400, "La ville choisie n'appartient pas au pays courant de l'entreprise");

                    newCityId = existingCity.Id;
                    newCityName = existingCity.CityName;
                }
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
                        var newCity = new payzen_backend.Models.Referentiel.City
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

            // CNSS
            if (!string.IsNullOrWhiteSpace(dto.CnssNumber))
            {
                var cnssExists = await _db.Companies
                    .AnyAsync(c => c.CnssNumber == dto.CnssNumber.Trim() && c.DeletedAt == null && c.Id != id);

                if (cnssExists)
                    throw new ServiceException(409, "Une autre entreprise utilise déjà ce numéro CNSS");

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

            // ICE
            if (!string.IsNullOrWhiteSpace(dto.IceNumber))
            {
                var iceExists = await _db.Companies
                    .AnyAsync(c => c.IceNumber == dto.IceNumber.Trim() && c.DeletedAt == null && c.Id != id);

                if (iceExists)
                    throw new ServiceException(409, "Une autre entreprise utilise déjà ce numéro ICE");

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

            // IF
            if (!string.IsNullOrWhiteSpace(dto.IfNumber))
            {
                var ifExists = await _db.Companies
                    .AnyAsync(c => c.IfNumber == dto.IfNumber.Trim() && c.DeletedAt == null && c.Id != id);
                if (ifExists)
                    throw new ServiceException(409, "Une autre entreprise utilise déjà ce numéro IF");
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

            // RC
            if (!string.IsNullOrWhiteSpace(dto.RcNumber))
            {
                var rcExists = await _db.Companies
                    .AnyAsync(c => c.RcNumber == dto.RcNumber.Trim() && c.DeletedAt == null && c.Id != id);

                if (rcExists)
                    throw new ServiceException(409, "Une autre entreprise utilise déjà ce numéro RC");

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

            // RIB
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

            // Patente
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

            // Website
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

            // LegalForm
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

            // FoundingDate
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

            // Status
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

            // Signatory
            if (!string.IsNullOrWhiteSpace(dto.SignatoryName))
            {
                var trimmed = dto.SignatoryName.Trim();
                if (trimmed != company.SignatoryName)
                {
                    await _companyEventLogService.LogEventAsync(
                        company.Id, "SignatoryName_Changed",
                        company.SignatoryName, null, trimmed, null, currentUserId);
                    company.SignatoryName = trimmed;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.SignatoryTitle))
            {
                var trimmed = dto.SignatoryTitle.Trim();
                if (trimmed != company.SignatoryTitle)
                {
                    await _companyEventLogService.LogEventAsync(
                        company.Id, "SignatoryTitle_Changed",
                        company.SignatoryTitle, null, trimmed, null, currentUserId);
                    company.SignatoryTitle = trimmed;
                }
            }

            // Audit
            company.ModifiedAt = DateTimeOffset.UtcNow;
            company.ModifiedBy = currentUserId;

            await _db.SaveChangesAsync();

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
                CreatedAt = updated.CreatedAt.DateTime,
                SignatoryName = updated.SignatoryName,
                SignatoryTitle = updated.SignatoryTitle
            };

            return result;
        }

        public async Task<List<CompanyHistoryDto>> GetCompanyHistoryAsync(int companyId)
        {
            var exists = await _db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId && c.DeletedAt == null);
            if (!exists)
                throw new ServiceException(404, "Entreprise non trouvée");

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

            if (!companyEvents.Any())
                return new List<CompanyHistoryDto>();

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

            var history = companyEvents.Select(e =>
            {
                string? name = null;
                string? role = null;
                if (userMap.TryGetValue(e.CreatedBy, out var u))
                {
                    name = u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}" : u.Email;
                    userRoleMap.TryGetValue(u.Id, out role);
                }

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

            return history;
        }

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
