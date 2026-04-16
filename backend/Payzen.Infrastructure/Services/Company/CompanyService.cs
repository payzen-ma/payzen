using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Auth;
using Payzen.Application.DTOs.Company;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Auth;
using Payzen.Domain.Entities.Company;
using Payzen.Domain.Entities.Referentiel;
using Payzen.Infrastructure.Hosting;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Company;

public class CompanyService : ICompanyService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ICompanyOnboardingService _onboarding;
    private readonly ICompanyEventLogService _companyEventLog;
    private readonly IInvitationService _invitationService;

    public CompanyService(
        AppDbContext db,
        IWebHostEnvironment env,
        ICompanyOnboardingService onboarding,
        ICompanyEventLogService companyEventLog,
        IInvitationService invitationService
    )
    {
        _db = db;
        _env = env;
        _onboarding = onboarding;
        _companyEventLog = companyEventLog;
        _invitationService = invitationService;
    }

    // ── Queries ──────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<CompanyListDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _db
            .Companies.Where(c => c.DeletedAt == null)
            .Include(c => c.City)
            .Include(c => c.Country)
            .OrderBy(c => c.CompanyName)
            .Select(c => MapToList(c))
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<CompanyListDto>>.Ok(list);
    }

    public async Task<ServiceResult<CompanyReadDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await _db
            .Companies.Include(c => c.City)
            .Include(c => c.Country)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, ct);
        return c == null
            ? ServiceResult<CompanyReadDto>.Fail("Entreprise non trouvée")
            : ServiceResult<CompanyReadDto>.Ok(MapToRead(c));
    }

    public async Task<ServiceResult<IEnumerable<CompanyListDto>>> GetByCityAsync(
        int cityId,
        CancellationToken ct = default
    )
    {
        var cityExists = await _db.Cities.AnyAsync(c => c.Id == cityId && c.DeletedAt == null, ct);
        if (!cityExists)
            return ServiceResult<IEnumerable<CompanyListDto>>.Fail("Ville non trouvée");
        var list = await _db
            .Companies.Include(c => c.City)
            .Include(c => c.Country)
            .Where(c => c.CityId == cityId && c.DeletedAt == null)
            .Select(c => MapToList(c))
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<CompanyListDto>>.Ok(list);
    }

    public async Task<ServiceResult<IEnumerable<CompanyListDto>>> GetByCountryAsync(
        int countryId,
        CancellationToken ct = default
    )
    {
        var countryExists = await _db.Countries.AnyAsync(c => c.Id == countryId && c.DeletedAt == null, ct);
        if (!countryExists)
            return ServiceResult<IEnumerable<CompanyListDto>>.Fail("Pays non trouvé");
        var list = await _db
            .Companies.Include(c => c.City)
            .Include(c => c.Country)
            .Where(c => c.CountryId == countryId && c.DeletedAt == null)
            .Select(c => MapToList(c))
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<CompanyListDto>>.Ok(list);
    }

    public async Task<ServiceResult<IEnumerable<CompanyListDto>>> SearchAsync(
        string searchTerm,
        CancellationToken ct = default
    )
    {
        var q = searchTerm.Trim().ToLower();
        var list = await _db
            .Companies.Include(c => c.City)
            .Include(c => c.Country)
            .Where(c =>
                c.DeletedAt == null
                && (
                    c.CompanyName.ToLower().Contains(q)
                    || c.Email.ToLower().Contains(q)
                    || (c.CnssNumber != null && c.CnssNumber.ToLower().Contains(q))
                )
            )
            .OrderBy(c => c.CompanyName)
            .Select(c => MapToList(c))
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<CompanyListDto>>.Ok(list);
    }

    public async Task<ServiceResult<IEnumerable<CompanyListDto>>> GetManagedByAsync(
        int expertCompanyId,
        CancellationToken ct = default
    )
    {
        var managing = await _db
            .Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == expertCompanyId && c.DeletedAt == null, ct);
        if (managing == null)
            return ServiceResult<IEnumerable<CompanyListDto>>.Fail("Cabinet gestionnaire introuvable");
        if (!managing.IsCabinetExpert)
            return ServiceResult<IEnumerable<CompanyListDto>>.Fail("La société spécifiée n'est pas un cabinet expert");
        var list = await _db
            .Companies.Include(c => c.City)
            .Include(c => c.Country)
            .Where(c => c.ManagedByCompanyId == expertCompanyId && c.DeletedAt == null)
            .Select(c => MapToList(c))
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<CompanyListDto>>.Ok(list);
    }

    public async Task<ServiceResult<CompanyFormDataDto>> GetFormDataAsync(CancellationToken ct = default)
    {
        var countries = await _db
            .Countries.Where(c => c.DeletedAt == null)
            .OrderBy(c => c.CountryName)
            .Select(c => new CountryFormDto
            {
                Id = c.Id,
                CountryName = c.CountryName,
                CountryNameAr = c.CountryNameAr,
                CountryCode = c.CountryCode,
                CountryPhoneCode = c.CountryPhoneCode,
            })
            .ToListAsync(ct);
        var cities = await _db
            .Cities.Where(c => c.DeletedAt == null)
            .Include(c => c.Country)
            .OrderBy(c => c.Country != null ? c.Country.CountryName : "")
            .ThenBy(c => c.CityName)
            .Select(c => new CityFormDto
            {
                Id = c.Id,
                CityName = c.CityName,
                CountryId = c.CountryId,
                CountryName = c.Country != null ? c.Country.CountryName : null,
            })
            .ToListAsync(ct);
        return ServiceResult<CompanyFormDataDto>.Ok(new CompanyFormDataDto { Countries = countries, Cities = cities });
    }

    public async Task<bool> CountryExistsAsync(int countryId, CancellationToken ct = default) =>
        await _db.Countries.AnyAsync(c => c.Id == countryId && c.DeletedAt == null, ct);

    public async Task<bool> CityExistsForCountryAsync(int cityId, int countryId, CancellationToken ct = default) =>
        await _db.Cities.AnyAsync(c => c.Id == cityId && c.CountryId == countryId && c.DeletedAt == null, ct);

    // ── Commands ─────────────────────────────────────────────────────────────
    // Validate uniqueness, create company, onboarding, fiche employé admin, invitation e-mail (Entra).

    public async Task<ServiceResult<CompanyCreateResponseDto>> CreateAsync(
        CompanyCreateDto dto,
        int createdBy,
        CancellationToken ct = default,
        bool sendInvitation = true,
        int? existingAdminUserId = null,
        bool createAdminAccount = true
    )
    {
        // Ville et pays sont validés par CompanyCreateValidator (pays existant, ville existante ou nom, pas les deux).

        var country = await _db
            .Countries.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == dto.CountryId && c.DeletedAt == null, ct);
        if (country == null)
            return ServiceResult<CompanyCreateResponseDto>.Fail("Pays non trouvé");

        // Vérifications unicité entreprise
        if (await _db.Companies.AnyAsync(c => c.Email == dto.CompanyEmail && c.DeletedAt == null, ct))
            return ServiceResult<CompanyCreateResponseDto>.Fail("Une entreprise avec cet email existe déjà");

        if (await _db.Companies.AnyAsync(c => c.CnssNumber == dto.CnssNumber && c.DeletedAt == null, ct))
            return ServiceResult<CompanyCreateResponseDto>.Fail("Une entreprise avec ce numéro CNSS existe déjà");

        // Vérifications unicité admin email (uniquement si on crée un admin pour la société)
        if (
            createAdminAccount
            && await _db.Employees.AnyAsync(e => e.Email == dto.AdminEmail && e.DeletedAt == null, ct)
        )
            return ServiceResult<CompanyCreateResponseDto>.Fail("Un employé avec cet email existe déjà");

        var existingUserWithAdminEmail = createAdminAccount
            ? await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.AdminEmail && u.DeletedAt == null, ct)
            : null;
        if (createAdminAccount && existingUserWithAdminEmail != null)
        {
            var isExpectedExistingUser =
                existingAdminUserId.HasValue && existingUserWithAdminEmail.Id == existingAdminUserId.Value;
            if (!isExpectedExistingUser)
                return ServiceResult<CompanyCreateResponseDto>.Fail("Un utilisateur avec cet email existe déjà");
        }

        // ---------- déterminer / créer la ville ----------
        int cityId;
        if (dto.CityId is > 0)
        {
            cityId = dto.CityId.Value;
        }
        else
        {
            // chercher doublon par nom (insensible casse)
            var duplicateCity = await _db.Cities.FirstOrDefaultAsync(
                c =>
                    c.CountryId == dto.CountryId
                    && c.CityName.ToLower() == dto.CityName!.ToLower()
                    && c.DeletedAt == null,
                ct
            );

            if (duplicateCity != null)
            {
                cityId = duplicateCity.Id;
            }
            else
            {
                var city = new City
                {
                    CityName = dto.CityName!.Trim(),
                    CountryId = dto.CountryId,
                    CreatedBy = createdBy,
                };
                _db.Cities.Add(city);
                await _db.SaveChangesAsync(ct);
                cityId = city.Id;
            }
        }

        // ---------- transaction pour atomicité ----------
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // ---------- créer l'entreprise ----------
            var company = new Domain.Entities.Company.Company
            {
                CompanyName = dto.CompanyName.Trim(),
                Email = dto.CompanyEmail.Trim(),
                PhoneNumber = dto.CompanyPhoneNumber.Trim(),
                CountryPhoneCode = dto.CountryPhoneCode ?? country.CountryPhoneCode,
                CompanyAddress = dto.CompanyAddress.Trim(),
                CityId = cityId,
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
                BusinessSector = dto.BusinessSector?.Trim(),
                PaymentMethod = dto.PaymentMethod?.Trim(),
                AuthType = "C",
                CreatedBy = createdBy,
            };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync(ct);

            // ---------- onboarding (seed defaults) ----------
            await _onboarding.OnboardAsync(company.Id, createdBy, ct);

            // ---------- récupérer statut "active" pour l'employé admin ----------
            var activeStatus = await _db.Statuses.FirstOrDefaultAsync(
                s => s.Code.ToLower() == "active" && s.DeletedAt == null,
                ct
            );
            if (activeStatus == null)
                return ServiceResult<CompanyCreateResponseDto>.Fail(
                    "Le statut 'Active' est introuvable dans la base de données"
                );

            Domain.Entities.Employee.Employee? adminEmployee = null;
            Domain.Entities.Auth.Roles? adminRole = null;

            if (createAdminAccount)
            {
                // ---------- créer l'employé admin ----------
                adminEmployee = new Domain.Entities.Employee.Employee
                {
                    FirstName = dto.AdminFirstName.Trim(),
                    LastName = dto.AdminLastName.Trim(),
                    Email = dto.AdminEmail.Trim(),
                    Phone = dto.AdminPhone.Trim(),
                    CompanyId = company.Id,
                    StatusId = activeStatus.Id,
                    CinNumber = "TEMP-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)), // temporaire
                    CreatedBy = createdBy,
                };
                _db.Employees.Add(adminEmployee);
                await _db.SaveChangesAsync(ct);

                adminRole = await _db.Roles.FirstOrDefaultAsync(
                    r => r.Name.ToLower() == "admin" && r.DeletedAt == null,
                    ct
                );
                if (adminRole == null)
                {
                    adminRole = new Domain.Entities.Auth.Roles
                    {
                        Name = "Admin",
                        Description = "Administrateur de l'entreprise",
                        CreatedBy = createdBy,
                    };
                    _db.Roles.Add(adminRole);
                    await _db.SaveChangesAsync(ct);
                }

                if (sendInvitation)
                {
                    await _invitationService.CreateInvitationAsync(
                        new InviteAdminDto
                        {
                            Email = dto.AdminEmail.Trim(),
                            CompanyId = company.Id,
                            RoleId = adminRole.Id,
                        },
                        ct
                    );
                }
            }

            await tx.CommitAsync(ct);

            // ---------- préparer la réponse ----------
            var response = new CompanyCreateResponseDto
            {
                Company = new CompanyReadDto
                {
                    Id = company.Id,
                    CompanyName = company.CompanyName,
                    Email = company.Email,
                    PhoneNumber = company.PhoneNumber,
                    CountryPhoneCode = company.CountryPhoneCode,
                    CompanyAddress = company.CompanyAddress,
                    CityId = company.CityId,
                    CityName = (await _db.Cities.FindAsync(new object[] { company.CityId }, ct))?.CityName,
                    CountryId = company.CountryId,
                    CountryName = (await _db.Countries.FindAsync(new object[] { company.CountryId }, ct))?.CountryName,
                    CnssNumber = company.CnssNumber,
                    IsCabinetExpert = company.IsCabinetExpert,
                    IceNumber = company.IceNumber,
                    IfNumber = company.IfNumber,
                    RcNumber = company.RcNumber,
                    LegalForm = company.LegalForm,
                    FoundingDate = company.FoundingDate,
                    BusinessSector = company.BusinessSector,
                    CreatedAt = company.CreatedAt.DateTime,
                },
                Admin =
                    createAdminAccount && adminEmployee != null
                        ? new AdminAccountDto
                        {
                            EmployeeId = adminEmployee.Id,
                            UserId = null,
                            Username = null,
                            Email = adminEmployee.Email,
                            FirstName = adminEmployee.FirstName,
                            LastName = adminEmployee.LastName,
                            Phone = adminEmployee.Phone,
                            Password = null,
                            Message = sendInvitation
                                ? "Une invitation a été envoyée à l'adresse indiquée. L'administrateur activera son compte via Microsoft Entra (lien dans l'e-mail ou logs si mode mock)."
                                : "Inscription finalisée. Le compte administrateur est activé via la session Microsoft Entra en cours.",
                        }
                        : new AdminAccountDto
                        {
                            EmployeeId = 0,
                            UserId = null,
                            Username = null,
                            Email = string.Empty,
                            FirstName = string.Empty,
                            LastName = string.Empty,
                            Phone = string.Empty,
                            Password = null,
                            Message = "Société créée sans compte administrateur (création par cabinet expert).",
                        },
            };

            return ServiceResult<CompanyCreateResponseDto>.Ok(response);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ServiceResult<CompanyCreateResponseDto>> CreateByExpertAsync(
        CompanyCreateByExpertDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var managing = await _db
            .Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == dto.ManagedByCompanyId && c.DeletedAt == null, ct);
        if (managing == null)
            return ServiceResult<CompanyCreateResponseDto>.Fail("Cabinet expert gestionnaire introuvable");
        if (!managing.IsCabinetExpert)
            return ServiceResult<CompanyCreateResponseDto>.Fail(
                "La société spécifiée comme gestionnaire n'est pas un cabinet expert"
            );

        var currentUser = await _db
            .Users.Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == createdBy && u.DeletedAt == null, ct);
        var currentEmployee = currentUser?.Employee;
        if (currentUser == null || currentEmployee == null || currentEmployee.CompanyId != dto.ManagedByCompanyId)
            return ServiceResult<CompanyCreateResponseDto>.Fail(
                "L'utilisateur courant n'appartient pas au cabinet gestionnaire ou est invalide"
            );

        var result = await CreateAsync(
            dto,
            createdBy,
            ct,
            sendInvitation: false,
            existingAdminUserId: null,
            createAdminAccount: false
        );
        if (!result.Success)
            return result;
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == result.Data!.Company.Id, ct);
        if (company != null)
        {
            company.ManagedByCompanyId = dto.ManagedByCompanyId;
            await _db.SaveChangesAsync(ct);
        }
        return result;
    }

    public async Task<ServiceResult<CompanyReadDto>> PatchAsync(
        int id,
        CompanyUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        if (dto == null)
            return ServiceResult<CompanyReadDto>.Fail("Données de mise à jour requises");

        var c = await _db
            .Companies.Include(x => x.City)
            .Include(x => x.Country)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (c == null)
            return ServiceResult<CompanyReadDto>.Fail("Entreprise non trouvée");

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email.Trim() != c.Email)
        {
            var newEmail = dto.Email.Trim();
            if (await _db.Companies.AnyAsync(x => x.Email == newEmail && x.DeletedAt == null && x.Id != id, ct))
                return ServiceResult<CompanyReadDto>.Fail("Une autre entreprise utilise déjà cet email");
            await _companyEventLog.LogEventAsync(c.Id, "Email_Changed", c.Email, null, newEmail, null, updatedBy, ct);
            c.Email = newEmail;
        }

        if (!string.IsNullOrWhiteSpace(dto.CompanyName) && dto.CompanyName.Trim() != c.CompanyName)
        {
            var name = dto.CompanyName.Trim();
            if (
                await _db.Companies.AnyAsync(
                    x => x.CompanyName.ToLower() == name.ToLower() && x.DeletedAt == null && x.Id != id,
                    ct
                )
            )
                return ServiceResult<CompanyReadDto>.Fail("Une autre entreprise utilise déjà ce nom");
            await _companyEventLog.LogEventAsync(
                c.Id,
                "CompanyName_Changed",
                c.CompanyName,
                null,
                name,
                null,
                updatedBy,
                ct
            );
            c.CompanyName = name;
        }

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && dto.PhoneNumber.Trim() != c.PhoneNumber)
        {
            var phone = dto.PhoneNumber.Trim();
            if (await _db.Companies.AnyAsync(x => x.PhoneNumber == phone && x.DeletedAt == null && x.Id != id, ct))
                return ServiceResult<CompanyReadDto>.Fail("Une autre entreprise utilise déjà ce numéro de téléphone");
            await _companyEventLog.LogEventAsync(
                c.Id,
                "Phone_Changed",
                c.PhoneNumber,
                null,
                phone,
                null,
                updatedBy,
                ct
            );
            c.PhoneNumber = phone;
        }

        if (!string.IsNullOrWhiteSpace(dto.CompanyAddress) && dto.CompanyAddress.Trim() != c.CompanyAddress)
        {
            await _companyEventLog.LogEventAsync(
                c.Id,
                "Address_Changed",
                c.CompanyAddress,
                null,
                dto.CompanyAddress.Trim(),
                null,
                updatedBy,
                ct
            );
            c.CompanyAddress = dto.CompanyAddress.Trim();
        }

        if (dto.IsCabinetExpert.HasValue && dto.IsCabinetExpert.Value != c.IsCabinetExpert)
        {
            await _companyEventLog.LogSimpleEventAsync(
                c.Id,
                "IsCabinetExpert_Changed",
                c.IsCabinetExpert.ToString(),
                dto.IsCabinetExpert.Value.ToString(),
                updatedBy,
                ct
            );
            c.IsCabinetExpert = dto.IsCabinetExpert.Value;
        }

        if (dto.CountryId.HasValue || !string.IsNullOrWhiteSpace(dto.CountryName))
        {
            if (dto.CountryId.HasValue && (c.CountryId != dto.CountryId.Value))
            {
                var country = await _db
                    .Countries.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.CountryId.Value && x.DeletedAt == null, ct);
                if (country == null)
                    return ServiceResult<CompanyReadDto>.Fail("Pays non trouvé");
                await _companyEventLog.LogEventAsync(
                    c.Id,
                    "Country_Changed",
                    c.CountryId.ToString(),
                    c.CountryId,
                    country.CountryName,
                    country.Id,
                    updatedBy,
                    ct
                );
                c.CountryId = country.Id;
                c.CountryPhoneCode = country.CountryPhoneCode;
            }
            else if (!string.IsNullOrWhiteSpace(dto.CountryName))
            {
                var name = dto.CountryName.Trim();
                var country = await _db.Countries.FirstOrDefaultAsync(
                    x => x.CountryName.ToLower() == name.ToLower() && x.DeletedAt == null,
                    ct
                );
                if (country != null)
                {
                    if (c.CountryId != country.Id)
                    {
                        await _companyEventLog.LogEventAsync(
                            c.Id,
                            "Country_Changed",
                            c.CountryId.ToString(),
                            c.CountryId,
                            country.CountryName,
                            country.Id,
                            updatedBy,
                            ct
                        );
                        c.CountryId = country.Id;
                        c.CountryPhoneCode = country.CountryPhoneCode;
                    }
                }
                else
                {
                    var phoneCode = !string.IsNullOrWhiteSpace(dto.CountryPhoneCode)
                        ? dto.CountryPhoneCode.Trim()
                        : (c.CountryPhoneCode ?? "+000");
                    var code = new string(name.Where(char.IsLetter).Take(3).ToArray()).ToUpper();
                    if (string.IsNullOrWhiteSpace(code))
                        code = "UNK";
                    var newCountry = new Country
                    {
                        CountryName = name,
                        CountryCode = code,
                        CountryPhoneCode = phoneCode,
                        CreatedBy = updatedBy,
                    };
                    _db.Countries.Add(newCountry);
                    await _db.SaveChangesAsync(ct);
                    await _companyEventLog.LogEventAsync(
                        c.Id,
                        "Country_Changed",
                        c.CountryId.ToString(),
                        c.CountryId,
                        newCountry.CountryName,
                        newCountry.Id,
                        updatedBy,
                        ct
                    );
                    c.CountryId = newCountry.Id;
                    c.CountryPhoneCode = newCountry.CountryPhoneCode;
                }
            }
        }

        if (dto.CityId.HasValue || !string.IsNullOrWhiteSpace(dto.CityName))
        {
            if (c.CountryId <= 0)
                return ServiceResult<CompanyReadDto>.Fail("CountryId requis pour créer/rechercher une ville.");

            if (dto.CityId.HasValue)
            {
                if (c.CityId == dto.CityId.Value)
                { /* no change */
                }
                else
                {
                    var city = await _db
                        .Cities.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == dto.CityId.Value && x.DeletedAt == null, ct);
                    if (city == null)
                        return ServiceResult<CompanyReadDto>.Fail("Ville non trouvée");
                    if (city.CountryId != c.CountryId)
                        return ServiceResult<CompanyReadDto>.Fail(
                            "La ville choisie n'appartient pas au pays courant de l'entreprise"
                        );
                    await _companyEventLog.LogEventAsync(
                        c.Id,
                        "City_Changed",
                        c.CityId.ToString(),
                        c.CityId,
                        city.CityName,
                        city.Id,
                        updatedBy,
                        ct
                    );
                    c.CityId = city.Id;
                }
            }
            else
            {
                var cityName = dto.CityName!.Trim();
                var city = await _db.Cities.FirstOrDefaultAsync(
                    x =>
                        x.CityName.ToLower() == cityName.ToLower() && x.CountryId == c.CountryId && x.DeletedAt == null,
                    ct
                );
                if (city != null)
                {
                    if (c.CityId != city.Id)
                    {
                        await _companyEventLog.LogEventAsync(
                            c.Id,
                            "City_Changed",
                            c.CityId.ToString(),
                            c.CityId,
                            city.CityName,
                            city.Id,
                            updatedBy,
                            ct
                        );
                        c.CityId = city.Id;
                    }
                }
                else
                {
                    var newCity = new City
                    {
                        CityName = cityName,
                        CountryId = c.CountryId,
                        CreatedBy = updatedBy,
                    };
                    _db.Cities.Add(newCity);
                    await _db.SaveChangesAsync(ct);
                    await _companyEventLog.LogEventAsync(
                        c.Id,
                        "City_Changed",
                        c.CityId.ToString(),
                        c.CityId,
                        newCity.CityName,
                        newCity.Id,
                        updatedBy,
                        ct
                    );
                    c.CityId = newCity.Id;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.CnssNumber))
        {
            var cnss = dto.CnssNumber.Trim();
            if (await _db.Companies.AnyAsync(x => x.CnssNumber == cnss && x.DeletedAt == null && x.Id != id, ct))
                return ServiceResult<CompanyReadDto>.Fail("Une autre entreprise utilise déjà ce numéro CNSS");
            if (cnss != c.CnssNumber)
            {
                await _companyEventLog.LogEventAsync(
                    c.Id,
                    "Cnss_Changed",
                    c.CnssNumber,
                    null,
                    cnss,
                    null,
                    updatedBy,
                    ct
                );
                c.CnssNumber = cnss;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.IceNumber))
        {
            var ice = dto.IceNumber.Trim();
            if (await _db.Companies.AnyAsync(x => x.IceNumber == ice && x.DeletedAt == null && x.Id != id, ct))
                return ServiceResult<CompanyReadDto>.Fail("Une autre entreprise utilise déjà ce numéro ICE");
            if (ice != c.IceNumber)
            {
                await _companyEventLog.LogEventAsync(c.Id, "Ice_Changed", c.IceNumber, null, ice, null, updatedBy, ct);
                c.IceNumber = ice;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.IfNumber))
        {
            var iff = dto.IfNumber.Trim();
            if (await _db.Companies.AnyAsync(x => x.IfNumber == iff && x.DeletedAt == null && x.Id != id, ct))
                return ServiceResult<CompanyReadDto>.Fail("Une autre entreprise utilise déjà ce numéro IF");
            if (iff != c.IfNumber)
            {
                await _companyEventLog.LogEventAsync(c.Id, "If_Changed", c.IfNumber, null, iff, null, updatedBy, ct);
                c.IfNumber = iff;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.RcNumber))
        {
            var rc = dto.RcNumber.Trim();
            if (await _db.Companies.AnyAsync(x => x.RcNumber == rc && x.DeletedAt == null && x.Id != id, ct))
                return ServiceResult<CompanyReadDto>.Fail("Une autre entreprise utilise déjà ce numéro RC");
            if (rc != c.RcNumber)
            {
                await _companyEventLog.LogEventAsync(c.Id, "Rc_Changed", c.RcNumber, null, rc, null, updatedBy, ct);
                c.RcNumber = rc;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.RibNumber) && dto.RibNumber.Trim() != c.RibNumber)
        {
            await _companyEventLog.LogEventAsync(
                c.Id,
                "Rib_Changed",
                c.RibNumber,
                null,
                dto.RibNumber.Trim(),
                null,
                updatedBy,
                ct
            );
            c.RibNumber = dto.RibNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.PatenteNumber) && dto.PatenteNumber.Trim() != c.PatenteNumber)
        {
            await _companyEventLog.LogEventAsync(
                c.Id,
                "Patent_Changed",
                c.PatenteNumber,
                null,
                dto.PatenteNumber.Trim(),
                null,
                updatedBy,
                ct
            );
            c.PatenteNumber = dto.PatenteNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.WebsiteUrl) && dto.WebsiteUrl.Trim() != c.WebsiteUrl)
        {
            await _companyEventLog.LogEventAsync(
                c.Id,
                "WebsiteUrl_Changed",
                c.WebsiteUrl,
                null,
                dto.WebsiteUrl.Trim(),
                null,
                updatedBy,
                ct
            );
            c.WebsiteUrl = dto.WebsiteUrl.Trim();
        }

        if (!string.IsNullOrEmpty(dto.LegalForm) && dto.LegalForm!.Trim() != c.LegalForm)
        {
            await _companyEventLog.LogEventAsync(
                c.Id,
                "LegalForm_Changed",
                c.LegalForm,
                null,
                dto.LegalForm.Trim(),
                null,
                updatedBy,
                ct
            );
            c.LegalForm = dto.LegalForm.Trim();
        }

        if (dto.FoundingDate.HasValue && dto.FoundingDate.Value != c.FoundingDate)
        {
            await _companyEventLog.LogEventAsync(
                c.Id,
                "FoundingDate_Changed",
                c.FoundingDate?.ToString("o"),
                null,
                dto.FoundingDate.Value.ToString("o"),
                null,
                updatedBy,
                ct
            );
            c.FoundingDate = dto.FoundingDate.Value;
        }

        if (dto.IsActive.HasValue && dto.IsActive.Value != c.isActive)
        {
            await _companyEventLog.LogSimpleEventAsync(
                c.Id,
                "Status_Changed",
                c.isActive ? "Active" : "Inactive",
                dto.IsActive.Value ? "Active" : "Inactive",
                updatedBy,
                ct
            );
            c.isActive = dto.IsActive.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.SignatoryName))
            c.SignatoryName = dto.SignatoryName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.SignatoryTitle))
            c.SignatoryTitle = dto.SignatoryTitle.Trim();
        if (!string.IsNullOrWhiteSpace(dto.PayrollPeriodicity))
            c.PayrollPeriodicity = dto.PayrollPeriodicity.Trim();

        if (!string.IsNullOrWhiteSpace(dto.AuthType))
        {
            var authType = dto.AuthType.Trim();
            if (!string.Equals(authType, c.AuthType, System.StringComparison.OrdinalIgnoreCase))
            {
                await _companyEventLog.LogSimpleEventAsync(
                    c.Id,
                    "AuthType_Changed",
                    c.AuthType,
                    authType,
                    updatedBy,
                    ct
                );
                c.AuthType = authType;
            }
        }

        c.UpdatedAt = DateTimeOffset.UtcNow;
        c.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);

        await _db.Entry(c).Reference(x => x.City).LoadAsync(ct);
        await _db.Entry(c).Reference(x => x.Country).LoadAsync(ct);
        return ServiceResult<CompanyReadDto>.Ok(MapToRead(c));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var c = await _db.Companies.FindAsync(new object[] { id }, ct);
        if (c == null)
            return ServiceResult.Fail("Société introuvable.");
        c.DeletedAt = DateTimeOffset.UtcNow;
        c.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<IEnumerable<CompanyHistoryDto>>> GetHistoryAsync(
        int companyId,
        CancellationToken ct = default
    )
    {
        var exists = await _db.Companies.AsNoTracking().AnyAsync(c => c.Id == companyId && c.DeletedAt == null, ct);
        if (!exists)
            return ServiceResult<IEnumerable<CompanyHistoryDto>>.Fail("Entreprise non trouvée");

        var events = await _db
            .CompanyEventLogs.AsNoTracking()
            .Where(l => l.companyId == companyId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.eventName,
                l.oldValue,
                l.oldValueId,
                l.newValue,
                l.newValueId,
                l.CreatedAt,
                l.CreatedBy,
                l.employeeId,
            })
            .ToListAsync(ct);

        if (events.Count == 0)
            return ServiceResult<IEnumerable<CompanyHistoryDto>>.Ok(new List<CompanyHistoryDto>());

        var creatorIds = events.Select(e => e.CreatedBy).Where(id => id != 0).Distinct().ToList();
        var users = await _db
            .Users.AsNoTracking()
            .Include(u => u.Employee)
            .Where(u => creatorIds.Contains(u.Id))
            .ToListAsync(ct);
        var usersRoles = await _db
            .UsersRoles.AsNoTracking()
            .Include(ur => ur.Role)
            .Where(ur => creatorIds.Contains(ur.UserId) && ur.DeletedAt == null)
            .ToListAsync(ct);
        var roleByUser = usersRoles
            .GroupBy(ur => ur.UserId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.Role?.Name).Where(n => n != null)));
        var userMap = users.ToDictionary(u => u.Id);

        var history = events
            .Select(e =>
            {
                string? name = null,
                    role = null;
                if (userMap.TryGetValue(e.CreatedBy, out var u))
                {
                    name = u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}" : u.Email;
                    roleByUser.TryGetValue(u.Id, out role);
                }
                var title = e.eventName ?? "Événement";
                var description = BuildHistoryDescription(e.eventName, e.oldValue, e.newValue);
                return new CompanyHistoryDto
                {
                    Type = "company",
                    Title = title,
                    Date = e.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = description,
                    Details = new Dictionary<string, object?>
                    {
                        ["oldValue"] = e.oldValue,
                        ["oldValueId"] = e.oldValueId,
                        ["newValue"] = e.newValue,
                        ["newValueId"] = e.newValueId,
                        ["employeeId"] = e.employeeId,
                        ["source"] = "company",
                    },
                    ModifiedBy = (name != null || role != null) ? new ModifiedByDto { Name = name, Role = role } : null,
                    Timestamp = e.CreatedAt.ToString("o"),
                };
            })
            .ToList();

        return ServiceResult<IEnumerable<CompanyHistoryDto>>.Ok(history);
    }

    private static string BuildHistoryDescription(string? eventName, string? oldValue, string? newValue)
    {
        if (!string.IsNullOrEmpty(oldValue) || !string.IsNullOrEmpty(newValue))
        {
            var oldVal = string.IsNullOrEmpty(oldValue) ? "<vide>" : oldValue;
            var newVal = string.IsNullOrEmpty(newValue) ? "<vide>" : newValue;
            return $"{eventName ?? "Événement"} : {oldVal} → {newVal}";
        }
        return eventName ?? "Événement";
    }

    // ── Départements ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<DepartementReadDto>>> GetAllDepartementsAsync(
        CancellationToken ct = default
    )
    {
        var list = await _db
            .Departements.Where(d => d.DeletedAt == null)
            .Include(d => d.Company)
            .OrderBy(d => d.DepartementName)
            .Select(d => new DepartementReadDto
            {
                Id = d.Id,
                DepartementName = d.DepartementName,
                CompanyId = d.CompanyId,
                CompanyName = d.Company != null ? d.Company.CompanyName : "",
                CreatedAt = d.CreatedAt.DateTime,
            })
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<DepartementReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<DepartementReadDto>> GetDepartementByIdAsync(int id, CancellationToken ct = default)
    {
        var d = await _db
            .Departements.Where(x => x.Id == id && x.DeletedAt == null)
            .Include(x => x.Company)
            .Select(x => new DepartementReadDto
            {
                Id = x.Id,
                DepartementName = x.DepartementName,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.CompanyName : "",
                CreatedAt = x.CreatedAt.DateTime,
            })
            .FirstOrDefaultAsync(ct);
        return d == null
            ? ServiceResult<DepartementReadDto>.Fail("Département non trouvé")
            : ServiceResult<DepartementReadDto>.Ok(d);
    }

    public async Task<ServiceResult<IEnumerable<DepartementReadDto>>> GetDepartementsAsync(
        int companyId,
        CancellationToken ct = default
    )
    {
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == companyId && c.DeletedAt == null, ct);
        if (!companyExists)
            return ServiceResult<IEnumerable<DepartementReadDto>>.Fail("Société non trouvée");
        var list = await _db
            .Departements.Where(d => d.CompanyId == companyId && d.DeletedAt == null)
            .Include(d => d.Company)
            .OrderBy(d => d.DepartementName)
            .Select(d => new DepartementReadDto
            {
                Id = d.Id,
                DepartementName = d.DepartementName,
                CompanyId = d.CompanyId,
                CompanyName = d.Company != null ? d.Company.CompanyName : "",
                CreatedAt = d.CreatedAt.DateTime,
            })
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<DepartementReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<DepartementReadDto>> CreateDepartementAsync(
        DepartementCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null, ct);
        if (!companyExists)
            return ServiceResult<DepartementReadDto>.Fail("La société spécifiée n'existe pas");
        var nameExists = await _db.Departements.AnyAsync(
            d => d.CompanyId == dto.CompanyId && d.DepartementName == dto.DepartementName && d.DeletedAt == null,
            ct
        );
        if (nameExists)
            return ServiceResult<DepartementReadDto>.Fail("Un département avec ce nom existe déjà dans cette société");

        var dep = new Departement
        {
            DepartementName = dto.DepartementName,
            CompanyId = dto.CompanyId,
            CreatedBy = createdBy,
        };
        _db.Departements.Add(dep);
        await _db.SaveChangesAsync(ct);

        await _companyEventLog.LogEventAsync(
            dep.CompanyId,
            "Departement_Created",
            null,
            null,
            dep.DepartementName,
            dep.Id,
            createdBy,
            ct
        );

        var companyName =
            await _db.Companies.Where(c => c.Id == dep.CompanyId).Select(c => c.CompanyName).FirstOrDefaultAsync(ct)
            ?? "";
        return ServiceResult<DepartementReadDto>.Ok(
            new DepartementReadDto
            {
                Id = dep.Id,
                DepartementName = dep.DepartementName,
                CompanyId = dep.CompanyId,
                CompanyName = companyName,
                CreatedAt = dep.CreatedAt.DateTime,
            }
        );
    }

    public async Task<ServiceResult<DepartementReadDto>> UpdateDepartementAsync(
        int id,
        DepartementUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var dep = await _db.Departements.FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null, ct);
        if (dep == null)
            return ServiceResult<DepartementReadDto>.Fail("Département non trouvé");

        var oldName = dep.DepartementName;
        var oldCompanyId = dep.CompanyId;

        if (!string.IsNullOrWhiteSpace(dto.DepartementName) && dto.DepartementName != dep.DepartementName)
        {
            var nameExists = await _db.Departements.AnyAsync(
                d =>
                    d.CompanyId == dep.CompanyId
                    && d.DepartementName == dto.DepartementName
                    && d.Id != id
                    && d.DeletedAt == null,
                ct
            );
            if (nameExists)
                return ServiceResult<DepartementReadDto>.Fail(
                    "Un département avec ce nom existe déjà dans cette société"
                );
            dep.DepartementName = dto.DepartementName;
        }

        if (dto.CompanyId.HasValue && dto.CompanyId.Value != dep.CompanyId)
        {
            var companyExists = await _db.Companies.AnyAsync(
                c => c.Id == dto.CompanyId.Value && c.DeletedAt == null,
                ct
            );
            if (!companyExists)
                return ServiceResult<DepartementReadDto>.Fail("La société spécifiée n'existe pas");
            var hasEmployees = await _db.Employees.AnyAsync(e => e.DepartementId == id && e.DeletedAt == null, ct);
            if (hasEmployees)
                return ServiceResult<DepartementReadDto>.Fail(
                    "Impossible de changer la société car le département contient des employés"
                );
            var nameExistsInNew = await _db.Departements.AnyAsync(
                d =>
                    d.CompanyId == dto.CompanyId.Value
                    && d.DepartementName == dep.DepartementName
                    && d.Id != id
                    && d.DeletedAt == null,
                ct
            );
            if (nameExistsInNew)
                return ServiceResult<DepartementReadDto>.Fail(
                    "Un département avec ce nom existe déjà dans la société cible"
                );
            dep.CompanyId = dto.CompanyId.Value;
        }

        dep.UpdatedAt = DateTimeOffset.UtcNow;
        dep.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);

        if (oldName != dep.DepartementName)
            await _companyEventLog.LogEventAsync(
                dep.CompanyId,
                "DepartementName_Changed",
                oldName,
                null,
                dep.DepartementName,
                dep.Id,
                updatedBy,
                ct
            );

        if (oldCompanyId != dep.CompanyId)
        {
            await _companyEventLog.LogEventAsync(
                oldCompanyId,
                "Departement_Unlinked",
                dep.DepartementName,
                dep.Id,
                null,
                null,
                updatedBy,
                ct
            );
            await _companyEventLog.LogEventAsync(
                dep.CompanyId,
                "Departement_Linked",
                null,
                null,
                dep.DepartementName,
                dep.Id,
                updatedBy,
                ct
            );
        }

        var companyName =
            await _db.Companies.Where(c => c.Id == dep.CompanyId).Select(c => c.CompanyName).FirstOrDefaultAsync(ct)
            ?? "";
        return ServiceResult<DepartementReadDto>.Ok(
            new DepartementReadDto
            {
                Id = dep.Id,
                DepartementName = dep.DepartementName,
                CompanyId = dep.CompanyId,
                CompanyName = companyName,
                CreatedAt = dep.CreatedAt.DateTime,
            }
        );
    }

    public async Task<ServiceResult> DeleteDepartementAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var dep = await _db.Departements.FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null, ct);
        if (dep == null)
            return ServiceResult.Fail("Département non trouvé");
        var hasEmployees = await _db.Employees.AnyAsync(e => e.DepartementId == id && e.DeletedAt == null, ct);
        if (hasEmployees)
            return ServiceResult.Fail("Impossible de supprimer ce département car il contient des employés actifs");
        dep.DeletedAt = DateTimeOffset.UtcNow;
        dep.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── JobPositions ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<JobPositionReadDto>>> GetAllJobPositionsAsync(
        CancellationToken ct = default
    )
    {
        var list = await _db
            .JobPositions.Where(jp => jp.DeletedAt == null)
            .Include(jp => jp.Company)
            .OrderBy(jp => jp.Name)
            .Select(jp => new JobPositionReadDto
            {
                Id = jp.Id,
                Name = jp.Name,
                CompanyId = jp.CompanyId,
                CompanyName = jp.Company != null ? jp.Company.CompanyName : null,
                CreatedAt = jp.CreatedAt.DateTime,
            })
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<JobPositionReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<JobPositionReadDto>> GetJobPositionByIdAsync(int id, CancellationToken ct = default)
    {
        var jp = await _db
            .JobPositions.Where(x => x.Id == id && x.DeletedAt == null)
            .Include(x => x.Company)
            .Select(x => new JobPositionReadDto
            {
                Id = x.Id,
                Name = x.Name,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.CompanyName : null,
                CreatedAt = x.CreatedAt.DateTime,
            })
            .FirstOrDefaultAsync(ct);
        return jp == null
            ? ServiceResult<JobPositionReadDto>.Fail("Poste non trouvé")
            : ServiceResult<JobPositionReadDto>.Ok(jp);
    }

    public async Task<ServiceResult<IEnumerable<JobPositionReadDto>>> GetJobPositionsAsync(
        int companyId,
        CancellationToken ct = default
    )
    {
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == companyId && c.DeletedAt == null, ct);
        if (!companyExists)
            return ServiceResult<IEnumerable<JobPositionReadDto>>.Fail("Société non trouvée");
        var list = await _db
            .JobPositions.Where(jp => jp.CompanyId == companyId && jp.DeletedAt == null)
            .Include(jp => jp.Company)
            .OrderBy(jp => jp.Name)
            .Select(jp => new JobPositionReadDto
            {
                Id = jp.Id,
                Name = jp.Name,
                CompanyId = jp.CompanyId,
                CompanyName = jp.Company != null ? jp.Company.CompanyName : null,
                CreatedAt = jp.CreatedAt.DateTime,
            })
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<JobPositionReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<JobPositionReadDto>> CreateJobPositionAsync(
        JobPositionCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null, ct);
        if (!companyExists)
            return ServiceResult<JobPositionReadDto>.Fail("Société non trouvée");
        var nameExists = await _db.JobPositions.AnyAsync(
            jp => jp.Name == dto.Name && jp.CompanyId == dto.CompanyId && jp.DeletedAt == null,
            ct
        );
        if (nameExists)
            return ServiceResult<JobPositionReadDto>.Fail("Un poste avec ce nom existe déjà pour cette société");

        var jp = new JobPosition
        {
            Name = dto.Name,
            CompanyId = dto.CompanyId,
            CreatedBy = createdBy,
        };
        _db.JobPositions.Add(jp);
        await _db.SaveChangesAsync(ct);

        await _companyEventLog.LogEventAsync(
            jp.CompanyId,
            "JobPosition_Created",
            null,
            null,
            jp.Name,
            jp.Id,
            createdBy,
            ct
        );

        var companyName = await _db
            .Companies.Where(c => c.Id == jp.CompanyId)
            .Select(c => c.CompanyName)
            .FirstOrDefaultAsync(ct);
        return ServiceResult<JobPositionReadDto>.Ok(
            new JobPositionReadDto
            {
                Id = jp.Id,
                Name = jp.Name,
                CompanyId = jp.CompanyId,
                CompanyName = companyName,
                CreatedAt = jp.CreatedAt.DateTime,
            }
        );
    }

    public async Task<ServiceResult<JobPositionReadDto>> UpdateJobPositionAsync(
        int id,
        JobPositionUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var jp = await _db.JobPositions.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (jp == null)
            return ServiceResult<JobPositionReadDto>.Fail("Poste non trouvé");

        var oldName = jp.Name;
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != jp.Name)
        {
            var nameExists = await _db.JobPositions.AnyAsync(
                x => x.Name == dto.Name && x.CompanyId == jp.CompanyId && x.Id != id && x.DeletedAt == null,
                ct
            );
            if (nameExists)
                return ServiceResult<JobPositionReadDto>.Fail("Un poste avec ce nom existe déjà pour cette société");
            jp.Name = dto.Name;
        }

        jp.UpdatedAt = DateTimeOffset.UtcNow;
        jp.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);

        await _companyEventLog.LogEventAsync(
            jp.CompanyId,
            "JobPosition_Updated",
            oldName,
            jp.Id,
            jp.Name,
            jp.Id,
            updatedBy,
            ct
        );

        var companyName = await _db
            .Companies.Where(c => c.Id == jp.CompanyId)
            .Select(c => c.CompanyName)
            .FirstOrDefaultAsync(ct);
        return ServiceResult<JobPositionReadDto>.Ok(
            new JobPositionReadDto
            {
                Id = jp.Id,
                Name = jp.Name,
                CompanyId = jp.CompanyId,
                CompanyName = companyName,
                CreatedAt = jp.CreatedAt.DateTime,
            }
        );
    }

    public async Task<ServiceResult> DeleteJobPositionAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var jp = await _db.JobPositions.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (jp == null)
            return ServiceResult.Fail("Poste non trouvé");
        var hasContracts = await _db.EmployeeContracts.AnyAsync(
            ec => ec.JobPositionId == id && ec.DeletedAt == null,
            ct
        );
        if (hasContracts)
            return ServiceResult.Fail(
                "Impossible de supprimer ce poste car il est utilisé dans des contrats d'employés"
            );

        await _companyEventLog.LogSimpleEventAsync(jp.CompanyId, "JobPosition_Deleted", jp.Name, null, deletedBy, ct);
        jp.DeletedAt = DateTimeOffset.UtcNow;
        jp.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── ContractTypes ────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<ContractTypeReadDto>>> GetContractTypesAsync(
        int companyId,
        CancellationToken ct = default
    )
    {
        var list = await _db
            .ContractTypes.Where(ct2 => ct2.CompanyId == companyId && ct2.DeletedAt == null)
            .Select(c => new ContractTypeReadDto
            {
                Id = c.Id,
                ContractTypeName = c.ContractTypeName,
                CompanyId = c.CompanyId,
                LegalContractTypeId = c.LegalContractTypeId,
            })
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<ContractTypeReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<ContractTypeReadDto>> GetContractTypeByIdAsync(
        int id,
        CancellationToken ct = default
    )
    {
        var c = await _db.ContractTypes.FirstOrDefaultAsync(ct2 => ct2.Id == id && ct2.DeletedAt == null, ct);
        return c == null
            ? ServiceResult<ContractTypeReadDto>.Fail("Type de contrat introuvable.")
            : ServiceResult<ContractTypeReadDto>.Ok(
                new ContractTypeReadDto
                {
                    Id = c.Id,
                    ContractTypeName = c.ContractTypeName,
                    CompanyId = c.CompanyId,
                    LegalContractTypeId = c.LegalContractTypeId,
                }
            );
    }

    public async Task<ServiceResult<ContractTypeReadDto>> CreateContractTypeAsync(
        ContractTypeCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var ct2 = new ContractType
        {
            ContractTypeName = dto.ContractTypeName,
            CompanyId = dto.CompanyId,
            LegalContractTypeId = dto.LegalContractTypeId,
            StateEmploymentProgramId = dto.StateEmploymentProgramId,
            CreatedBy = createdBy,
        };
        _db.ContractTypes.Add(ct2);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<ContractTypeReadDto>.Ok(
            new ContractTypeReadDto
            {
                Id = ct2.Id,
                ContractTypeName = ct2.ContractTypeName,
                CompanyId = ct2.CompanyId,
            }
        );
    }

    public async Task<ServiceResult<ContractTypeReadDto>> UpdateContractTypeAsync(
        int id,
        ContractTypeUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var ct2 = await _db.ContractTypes.FindAsync(new object[] { id }, ct);
        if (ct2 == null)
            return ServiceResult<ContractTypeReadDto>.Fail("Type de contrat introuvable.");
        ct2.ContractTypeName = dto.ContractTypeName ?? ct2.ContractTypeName;
        ct2.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<ContractTypeReadDto>.Ok(
            new ContractTypeReadDto
            {
                Id = ct2.Id,
                ContractTypeName = ct2.ContractTypeName,
                CompanyId = ct2.CompanyId,
            }
        );
    }

    public async Task<ServiceResult> DeleteContractTypeAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var ct2 = await _db.ContractTypes.FindAsync(new object[] { id }, ct);
        if (ct2 == null)
            return ServiceResult.Fail("Type de contrat introuvable.");
        ct2.DeletedAt = DateTimeOffset.UtcNow;
        ct2.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── WorkingCalendar ──────────────────────────────────────────────────────
    // Parité payzen_backend WorkingCalendarsController (filtre DeletedAt, vérif société, conflit jour, horaires, DTO enrichi).

    private static readonly string[] DayNames =
    {
        "Dimanche",
        "Lundi",
        "Mardi",
        "Mercredi",
        "Jeudi",
        "Vendredi",
        "Samedi",
    };

    private static WorkingCalendarReadDto MapWorkingCalendarRead(WorkingCalendar wc)
    {
        var dow = wc.DayOfWeek;
        if (dow < 0 || dow > 6)
            dow = 0;
        return new WorkingCalendarReadDto
        {
            Id = wc.Id,
            CompanyId = wc.CompanyId,
            CompanyName = wc.Company?.CompanyName ?? string.Empty,
            DayOfWeek = wc.DayOfWeek,
            DayOfWeekName = DayNames[dow],
            IsWorkingDay = wc.IsWorkingDay,
            StartTime = wc.StartTime,
            EndTime = wc.EndTime,
            CreatedAt = wc.CreatedAt.UtcDateTime,
        };
    }

    public async Task<ServiceResult<IEnumerable<WorkingCalendarReadDto>>> GetAllWorkingCalendarsAsync(
        CancellationToken ct = default
    )
    {
        var list = await _db
            .WorkingCalendars.AsNoTracking()
            .Where(wc => wc.DeletedAt == null)
            .Include(wc => wc.Company)
            .OrderBy(wc => wc.CompanyId)
            .ThenBy(wc => wc.DayOfWeek)
            .ToListAsync(ct);

        return ServiceResult<IEnumerable<WorkingCalendarReadDto>>.Ok(list.Select(MapWorkingCalendarRead).ToList());
    }

    public async Task<ServiceResult<IEnumerable<WorkingCalendarReadDto>>> GetWorkingCalendarAsync(
        int companyId,
        CancellationToken ct = default
    )
    {
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == companyId && c.DeletedAt == null, ct);
        if (!companyExists)
            return ServiceResult<IEnumerable<WorkingCalendarReadDto>>.Fail("Société non trouvée");

        var list = await _db
            .WorkingCalendars.AsNoTracking()
            .Where(wc => wc.CompanyId == companyId && wc.DeletedAt == null)
            .Include(wc => wc.Company)
            .OrderBy(wc => wc.DayOfWeek)
            .ToListAsync(ct);

        return ServiceResult<IEnumerable<WorkingCalendarReadDto>>.Ok(list.Select(MapWorkingCalendarRead).ToList());
    }

    public async Task<ServiceResult<WorkingCalendarReadDto>> UpsertWorkingCalendarDayAsync(
        WorkingCalendarCreateDto dto,
        int userId,
        CancellationToken ct = default
    )
    {
        // Parité monolithe : création uniquement (409 si jour déjà défini pour la société), pas de mise à jour silencieuse.
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null, ct);
        if (!companyExists)
            return ServiceResult<WorkingCalendarReadDto>.Fail("Société non trouvée");

        var duplicate = await _db.WorkingCalendars.AnyAsync(
            wc => wc.CompanyId == dto.CompanyId && wc.DayOfWeek == dto.DayOfWeek && wc.DeletedAt == null,
            ct
        );
        if (duplicate)
            return ServiceResult<WorkingCalendarReadDto>.Fail(
                "Un calendrier existe déjà pour ce jour de la semaine dans cette société"
            );

        if (dto.IsWorkingDay)
        {
            if (dto.StartTime == null || dto.EndTime == null)
                return ServiceResult<WorkingCalendarReadDto>.Fail(
                    "Les horaires de début et de fin sont requis pour un jour travaillé"
                );
            if (dto.StartTime >= dto.EndTime)
                return ServiceResult<WorkingCalendarReadDto>.Fail("L'heure de début doit être avant l'heure de fin");
        }

        var wc = new WorkingCalendar
        {
            CompanyId = dto.CompanyId,
            DayOfWeek = dto.DayOfWeek,
            IsWorkingDay = dto.IsWorkingDay,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            CreatedBy = userId,
        };
        _db.WorkingCalendars.Add(wc);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(wc).Reference(x => x.Company).LoadAsync(ct);
        return ServiceResult<WorkingCalendarReadDto>.Ok(MapWorkingCalendarRead(wc));
    }

    public async Task<ServiceResult<WorkingCalendarReadDto>> UpdateWorkingCalendarDayAsync(
        int id,
        WorkingCalendarUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var wc = await _db.WorkingCalendars.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (wc == null)
            return ServiceResult<WorkingCalendarReadDto>.Fail("Calendrier de travail non trouvé");

        if (dto.CompanyId.HasValue && dto.CompanyId.Value != wc.CompanyId)
        {
            var ok = await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId.Value && c.DeletedAt == null, ct);
            if (!ok)
                return ServiceResult<WorkingCalendarReadDto>.Fail("Société non trouvée");
            wc.CompanyId = dto.CompanyId.Value;
        }

        if (dto.DayOfWeek.HasValue && dto.DayOfWeek.Value != wc.DayOfWeek)
        {
            var compId = dto.CompanyId ?? wc.CompanyId;
            var dayTaken = await _db.WorkingCalendars.AnyAsync(
                x => x.CompanyId == compId && x.DayOfWeek == dto.DayOfWeek.Value && x.Id != id && x.DeletedAt == null,
                ct
            );
            if (dayTaken)
                return ServiceResult<WorkingCalendarReadDto>.Fail(
                    "Un calendrier existe déjà pour ce jour de la semaine dans cette société"
                );
            wc.DayOfWeek = dto.DayOfWeek.Value;
        }

        if (dto.IsWorkingDay.HasValue)
        {
            wc.IsWorkingDay = dto.IsWorkingDay.Value;
            if (dto.IsWorkingDay.Value)
            {
                var startTime = dto.StartTime ?? wc.StartTime;
                var endTime = dto.EndTime ?? wc.EndTime;
                if (startTime == null || endTime == null)
                    return ServiceResult<WorkingCalendarReadDto>.Fail(
                        "Les horaires de début et de fin sont requis pour un jour travaillé"
                    );
                if (startTime >= endTime)
                    return ServiceResult<WorkingCalendarReadDto>.Fail(
                        "L'heure de début doit être avant l'heure de fin"
                    );
            }
        }

        if (dto.StartTime.HasValue)
        {
            if (wc.IsWorkingDay)
            {
                var endTime = dto.EndTime ?? wc.EndTime;
                if (endTime != null && dto.StartTime >= endTime)
                    return ServiceResult<WorkingCalendarReadDto>.Fail(
                        "L'heure de début doit être avant l'heure de fin"
                    );
            }
            wc.StartTime = dto.StartTime;
        }

        if (dto.EndTime.HasValue)
        {
            if (wc.IsWorkingDay)
            {
                var startTime = dto.StartTime ?? wc.StartTime;
                if (startTime != null && startTime >= dto.EndTime)
                    return ServiceResult<WorkingCalendarReadDto>.Fail(
                        "L'heure de début doit être avant l'heure de fin"
                    );
            }
            wc.EndTime = dto.EndTime;
        }

        wc.UpdatedAt = DateTimeOffset.UtcNow;
        wc.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);

        await _db.Entry(wc).Reference(x => x.Company).LoadAsync(ct);
        return ServiceResult<WorkingCalendarReadDto>.Ok(MapWorkingCalendarRead(wc));
    }

    public async Task<ServiceResult<WorkingCalendarReadDto>> GetWorkingCalendarDayByIdAsync(
        int id,
        CancellationToken ct = default
    )
    {
        var wc = await _db
            .WorkingCalendars.AsNoTracking()
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (wc == null)
            return ServiceResult<WorkingCalendarReadDto>.Fail("Calendrier de travail non trouvé");
        return ServiceResult<WorkingCalendarReadDto>.Ok(MapWorkingCalendarRead(wc));
    }

    public async Task<ServiceResult> DeleteWorkingCalendarDayAsync(
        int id,
        int deletedBy,
        CancellationToken ct = default
    )
    {
        var wc = await _db.WorkingCalendars.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (wc == null)
            return ServiceResult.Fail("Calendrier de travail non trouvé");
        wc.DeletedAt = DateTimeOffset.UtcNow;
        wc.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Holidays ─────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<HolidayReadDto>>> GetHolidaysAsync(
        int? companyId,
        int? year,
        CancellationToken ct = default
    )
    {
        var query = _db.Holidays.Where(h => h.DeletedAt == null).AsQueryable();
        if (companyId.HasValue)
            query = query.Where(h => h.CompanyId == companyId || h.CompanyId == null);
        if (year.HasValue)
            query = query.Where(h => h.HolidayDate.Year == year.Value);
        var list = await query
            .OrderBy(h => h.HolidayDate)
            .Select(h => new HolidayReadDto
            {
                Id = h.Id,
                NameFr = h.NameFr,
                NameAr = h.NameAr,
                NameEn = h.NameEn,
                HolidayDate = h.HolidayDate,
                Description = h.Description,
                CompanyId = h.CompanyId,
                CountryId = h.CountryId,
                Scope = h.Scope,
                ScopeDescription = h.Scope.ToString(),
                HolidayType = h.HolidayType,
                IsMandatory = h.IsMandatory,
                IsPaid = h.IsPaid,
                IsRecurring = h.IsRecurring,
                RecurrenceRule = h.RecurrenceRule,
                Year = h.Year,
                AffectPayroll = h.AffectPayroll,
                AffectAttendance = h.AffectAttendance,
                IsActive = h.IsActive,
                CreatedAt = h.CreatedAt.UtcDateTime,
            })
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<HolidayReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<HolidayReadDto>> GetHolidayByIdAsync(int id, CancellationToken ct = default)
    {
        var h = await _db.Holidays.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (h == null)
            return ServiceResult<HolidayReadDto>.Fail("Férié introuvable.");
        return ServiceResult<HolidayReadDto>.Ok(
            new HolidayReadDto
            {
                Id = h.Id,
                NameFr = h.NameFr,
                NameAr = h.NameAr,
                NameEn = h.NameEn,
                HolidayDate = h.HolidayDate,
                Description = h.Description,
                CompanyId = h.CompanyId,
                CountryId = h.CountryId,
                Scope = h.Scope,
                ScopeDescription = h.Scope.ToString(),
                HolidayType = h.HolidayType,
                IsMandatory = h.IsMandatory,
                IsPaid = h.IsPaid,
                IsRecurring = h.IsRecurring,
                RecurrenceRule = h.RecurrenceRule,
                Year = h.Year,
                AffectPayroll = h.AffectPayroll,
                AffectAttendance = h.AffectAttendance,
                IsActive = h.IsActive,
                CreatedAt = h.CreatedAt.UtcDateTime,
            }
        );
    }

    public async Task<ServiceResult<bool>> CheckHolidayAsync(
        int? companyId,
        DateOnly date,
        CancellationToken ct = default
    )
    {
        var q = _db.Holidays.Where(h => h.DeletedAt == null && h.HolidayDate == date);
        if (companyId.HasValue)
            q = q.Where(h => h.CompanyId == companyId || h.CompanyId == null);
        var isHoliday = await q.AnyAsync(ct);
        return ServiceResult<bool>.Ok(isHoliday);
    }

    public async Task<ServiceResult<IEnumerable<object>>> GetHolidayTypesAsync(CancellationToken ct = default)
    {
        var types = new[]
        {
            new { id = "National", name = "National" },
            new { id = "Religieux", name = "Religieux" },
            new { id = "Autre", name = "Autre" },
        };
        return ServiceResult<IEnumerable<object>>.Ok(types);
    }

    public async Task<ServiceResult<HolidayReadDto>> CreateHolidayAsync(
        HolidayCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var h = new Holiday
        {
            NameFr = dto.NameFr,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            HolidayDate = dto.HolidayDate,
            Description = dto.Description,
            CompanyId = dto.CompanyId,
            CountryId = dto.CountryId,
            Scope = dto.Scope,
            HolidayType = dto.HolidayType ?? string.Empty,
            IsMandatory = dto.IsMandatory,
            IsPaid = dto.IsPaid,
            IsRecurring = dto.IsRecurring,
            RecurrenceRule = dto.RecurrenceRule,
            Year = dto.Year,
            AffectPayroll = dto.AffectPayroll,
            AffectAttendance = dto.AffectAttendance,
            IsActive = dto.IsActive,
            CreatedBy = createdBy,
        };
        _db.Holidays.Add(h);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<HolidayReadDto>.Ok(
            new HolidayReadDto
            {
                Id = h.Id,
                NameFr = h.NameFr,
                NameAr = h.NameAr,
                NameEn = h.NameEn,
                HolidayDate = h.HolidayDate,
                Description = h.Description,
                CompanyId = h.CompanyId,
                CountryId = h.CountryId,
                Scope = h.Scope,
                ScopeDescription = h.Scope.ToString(),
                HolidayType = h.HolidayType,
                IsMandatory = h.IsMandatory,
                IsPaid = h.IsPaid,
                IsRecurring = h.IsRecurring,
                RecurrenceRule = h.RecurrenceRule,
                Year = h.Year,
                AffectPayroll = h.AffectPayroll,
                AffectAttendance = h.AffectAttendance,
                IsActive = h.IsActive,
                CreatedAt = h.CreatedAt.UtcDateTime,
            }
        );
    }

    public async Task<ServiceResult<HolidayReadDto>> UpdateHolidayAsync(
        int id,
        HolidayUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var h = await _db.Holidays.FindAsync(new object[] { id }, ct);
        if (h == null)
            return ServiceResult<HolidayReadDto>.Fail("Férié introuvable.");
        if (dto.NameFr != null)
            h.NameFr = dto.NameFr;
        if (dto.NameAr != null)
            h.NameAr = dto.NameAr;
        if (dto.NameEn != null)
            h.NameEn = dto.NameEn;
        if (dto.HolidayDate.HasValue)
            h.HolidayDate = dto.HolidayDate.Value;
        if (dto.Description != null)
            h.Description = dto.Description;
        if (dto.CountryId.HasValue)
            h.CountryId = dto.CountryId.Value;
        if (dto.Scope.HasValue)
            h.Scope = dto.Scope.Value;
        if (dto.HolidayType != null)
            h.HolidayType = dto.HolidayType;
        if (dto.IsMandatory.HasValue)
            h.IsMandatory = dto.IsMandatory.Value;
        if (dto.IsPaid.HasValue)
            h.IsPaid = dto.IsPaid.Value;
        if (dto.IsRecurring.HasValue)
            h.IsRecurring = dto.IsRecurring.Value;
        if (dto.RecurrenceRule != null)
            h.RecurrenceRule = dto.RecurrenceRule;
        if (dto.Year.HasValue)
            h.Year = dto.Year.Value;
        if (dto.AffectPayroll.HasValue)
            h.AffectPayroll = dto.AffectPayroll.Value;
        if (dto.AffectAttendance.HasValue)
            h.AffectAttendance = dto.AffectAttendance.Value;
        if (dto.IsActive.HasValue)
            h.IsActive = dto.IsActive.Value;
        h.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<HolidayReadDto>.Ok(
            new HolidayReadDto
            {
                Id = h.Id,
                NameFr = h.NameFr,
                NameAr = h.NameAr,
                NameEn = h.NameEn,
                HolidayDate = h.HolidayDate,
                Description = h.Description,
                CompanyId = h.CompanyId,
                CountryId = h.CountryId,
                Scope = h.Scope,
                ScopeDescription = h.Scope.ToString(),
                HolidayType = h.HolidayType,
                IsMandatory = h.IsMandatory,
                IsPaid = h.IsPaid,
                IsRecurring = h.IsRecurring,
                RecurrenceRule = h.RecurrenceRule,
                Year = h.Year,
                AffectPayroll = h.AffectPayroll,
                AffectAttendance = h.AffectAttendance,
                IsActive = h.IsActive,
                CreatedAt = h.CreatedAt.UtcDateTime,
            }
        );
    }

    public async Task<ServiceResult> DeleteHolidayAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var h = await _db.Holidays.FindAsync(new object[] { id }, ct);
        if (h == null)
            return ServiceResult.Fail("Férié introuvable.");
        h.DeletedAt = DateTimeOffset.UtcNow;
        h.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── Documents ────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<CompanyDocumentReadDto>>> GetAllDocumentsAsync(
        CancellationToken ct = default
    )
    {
        var list = await _db
            .CompanyDocuments.AsNoTracking()
            .Where(d => d.DeletedAt == null)
            .Include(d => d.Company)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new CompanyDocumentReadDto
            {
                Id = d.Id,
                CompanyId = d.CompanyId,
                CompanyName = d.Company != null ? d.Company.CompanyName : "N/A",
                Name = d.Name,
                FilePath = d.FilePath,
                DocumentType = d.DocumentType,
                CreatedAt = d.CreatedAt.UtcDateTime,
            })
            .ToListAsync(ct);

        return ServiceResult<IEnumerable<CompanyDocumentReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<CompanyDocumentReadDto>> GetDocumentByIdAsync(
        int id,
        CancellationToken ct = default
    )
    {
        var d = await _db
            .CompanyDocuments.AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Include(x => x.Company)
            .FirstOrDefaultAsync(ct);

        if (d == null)
            return ServiceResult<CompanyDocumentReadDto>.Fail("Document non trouvé");

        return ServiceResult<CompanyDocumentReadDto>.Ok(
            new CompanyDocumentReadDto
            {
                Id = d.Id,
                CompanyId = d.CompanyId,
                CompanyName = d.Company != null ? d.Company.CompanyName : "N/A",
                Name = d.Name,
                FilePath = d.FilePath,
                DocumentType = d.DocumentType,
                CreatedAt = d.CreatedAt.UtcDateTime,
            }
        );
    }

    public async Task<ServiceResult<IEnumerable<CompanyDocumentReadDto>>> GetDocumentsAsync(
        int companyId,
        CancellationToken ct = default
    )
    {
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == companyId && c.DeletedAt == null, ct);
        if (!companyExists)
            return ServiceResult<IEnumerable<CompanyDocumentReadDto>>.Fail("Entreprise non trouvée");

        var list = await _db
            .CompanyDocuments.AsNoTracking()
            .Where(d => d.CompanyId == companyId && d.DeletedAt == null)
            .Include(d => d.Company)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new CompanyDocumentReadDto
            {
                Id = d.Id,
                CompanyId = d.CompanyId,
                CompanyName = d.Company != null ? d.Company.CompanyName : "N/A",
                Name = d.Name,
                FilePath = d.FilePath,
                DocumentType = d.DocumentType,
                CreatedAt = d.CreatedAt.UtcDateTime,
            })
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<CompanyDocumentReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<CompanyDocumentReadDto>> CreateDocumentAsync(
        CompanyDocumentCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null, ct);
        if (!companyExists)
            return ServiceResult<CompanyDocumentReadDto>.Fail("Entreprise non trouvée");

        var doc = new CompanyDocument
        {
            CompanyId = dto.CompanyId,
            Name = dto.Name,
            FilePath = dto.FilePath,
            DocumentType = dto.DocumentType,
            CreatedBy = createdBy,
        };
        _db.CompanyDocuments.Add(doc);
        await _db.SaveChangesAsync(ct);
        var companyName =
            await _db.Companies.Where(c => c.Id == doc.CompanyId).Select(c => c.CompanyName).FirstOrDefaultAsync(ct)
            ?? "N/A";
        return ServiceResult<CompanyDocumentReadDto>.Ok(
            new CompanyDocumentReadDto
            {
                Id = doc.Id,
                CompanyId = doc.CompanyId,
                CompanyName = companyName,
                Name = doc.Name,
                FilePath = doc.FilePath,
                DocumentType = doc.DocumentType,
                CreatedAt = doc.CreatedAt.UtcDateTime,
            }
        );
    }

    public async Task<ServiceResult<CompanyDocumentReadDto>> UpdateDocumentAsync(
        int id,
        CompanyDocumentUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var doc = await _db.CompanyDocuments.FindAsync(new object[] { id }, ct);
        if (doc == null || doc.DeletedAt != null)
            return ServiceResult<CompanyDocumentReadDto>.Fail("Document non trouvé");
        // Parité monolithe : ne pas écraser le nom avec une chaîne vide / blanche
        if (!string.IsNullOrWhiteSpace(dto.Name))
            doc.Name = dto.Name;
        if (dto.DocumentType != null)
            doc.DocumentType = dto.DocumentType;
        doc.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        var companyName =
            await _db.Companies.Where(c => c.Id == doc.CompanyId).Select(c => c.CompanyName).FirstOrDefaultAsync(ct)
            ?? "N/A";
        return ServiceResult<CompanyDocumentReadDto>.Ok(
            new CompanyDocumentReadDto
            {
                Id = doc.Id,
                CompanyId = doc.CompanyId,
                CompanyName = companyName,
                Name = doc.Name,
                FilePath = doc.FilePath,
                DocumentType = doc.DocumentType,
                CreatedAt = doc.CreatedAt.UtcDateTime,
            }
        );
    }

    public async Task<ServiceResult> DeleteDocumentAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var doc = await _db.CompanyDocuments.FindAsync(new object[] { id }, ct);
        if (doc == null || doc.DeletedAt != null)
            return ServiceResult.Fail("Document non trouvé");
        doc.DeletedAt = DateTimeOffset.UtcNow;
        doc.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    // ── File storage ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<string>> SaveFileAsync(
        IFormFile file,
        int companyId,
        string? documentType,
        CancellationToken ct = default
    )
    {
        var webRoot = WebRootPathHelper.ResolveWwwRoot(_env);
        var folder = Path.Combine(webRoot, "uploads", "companies", companyId.ToString());
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(folder, fileName);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);
        var relativePath = Path.Combine("uploads", "companies", companyId.ToString(), fileName).Replace("\\", "/");
        return ServiceResult<string>.Ok(relativePath);
    }

    public Task<ServiceResult> DeleteFileAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult(ServiceResult.Fail("Chemin de fichier manquant."));

        var webRoot = WebRootPathHelper.ResolveWwwRoot(_env);
        var full = Path.Combine(webRoot, filePath.TrimStart('/'));
        if (File.Exists(full))
            File.Delete(full);
        return Task.FromResult(ServiceResult.Ok());
    }

    public async Task<ServiceResult<(byte[] fileBytes, string contentType, string fileName)>> GetFileAsync(
        string filePath,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return ServiceResult<(byte[], string, string)>.Fail("Chemin de fichier manquant.");

        var webRoot = WebRootPathHelper.ResolveWwwRoot(_env);
        var full = Path.Combine(webRoot, filePath.TrimStart('/'));
        if (!File.Exists(full))
            return ServiceResult<(byte[], string, string)>.Fail("Fichier introuvable.");
        var bytes = await File.ReadAllBytesAsync(full, ct);
        var ext = Path.GetExtension(filePath).ToLower();
        var mimeType = ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream",
        };
        return ServiceResult<(byte[], string, string)>.Ok((bytes, mimeType, Path.GetFileName(filePath)));
    }

    // ── Private mappers ─────────────────────────────────────────────────────

    private static CompanyListDto MapToList(Domain.Entities.Company.Company c) =>
        new()
        {
            Id = c.Id,
            CompanyName = c.CompanyName,
            IsCabinetExpert = c.IsCabinetExpert,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            CountryPhoneCode = c.CountryPhoneCode,
            CityName = c.City?.CityName,
            CountryName = c.Country?.CountryName,
            CompanyAddress = c.CompanyAddress,
            CnssNumber = c.CnssNumber,
            IceNumber = c.IceNumber,
            IfNumber = c.IfNumber,
            RcNumber = c.RcNumber,
            PatenteNumber = c.PatenteNumber,
            WebsiteUrl = c.WebsiteUrl,
            RibNumber = c.RibNumber,
            LegalForm = c.LegalForm,
            isActive = c.isActive,
            FoundingDate = c.FoundingDate,
            SignatoryName = c.SignatoryName,
            SignatoryTitle = c.SignatoryTitle,
            PayrollPeriodicity = c.PayrollPeriodicity,
            AuthType = c.AuthType,
            CreatedAt = c.CreatedAt.DateTime,
        };

    private static CompanyReadDto MapToRead(Domain.Entities.Company.Company c) =>
        new()
        {
            Id = c.Id,
            CompanyName = c.CompanyName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            CountryPhoneCode = c.CountryPhoneCode,
            CompanyAddress = c.CompanyAddress,
            CityId = c.CityId,
            CityName = c.City?.CityName,
            CountryId = c.CountryId,
            CountryName = c.Country?.CountryName,
            CnssNumber = c.CnssNumber,
            IsCabinetExpert = c.IsCabinetExpert,
            IceNumber = c.IceNumber,
            IfNumber = c.IfNumber,
            RcNumber = c.RcNumber,
            LegalForm = c.LegalForm,
            WebsiteUrl = c.WebsiteUrl,
            PatentNumber = c.PatenteNumber,
            RibNumber = c.RibNumber,
            FoundingDate = c.FoundingDate,
            BusinessSector = c.BusinessSector,
            isActive = c.isActive,
            SignatoryName = c.SignatoryName,
            SignatoryTitle = c.SignatoryTitle,
            PayrollPeriodicity = c.PayrollPeriodicity,
            AuthType = c.AuthType,
            CreatedAt = c.CreatedAt.DateTime,
        };
}
