using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Company;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Auth;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Public;

public sealed class PublicSignupService : IPublicSignupService
{
    private readonly ICompanyService _companySvc;
    private readonly IValidator<CompanyCreateDto> _createValidator;
    private readonly AppDbContext _db;

    public PublicSignupService(
        ICompanyService companySvc,
        IValidator<CompanyCreateDto> createValidator,
        AppDbContext db)
    {
        _companySvc = companySvc;
        _createValidator = createValidator;
        _db = db;
    }

    public async Task<ServiceResult<object>> SignupCompanyAdminAsync(PublicSignupRequest request, CancellationToken ct)
    {
        // 1. Récupération du pays par défaut
        var fallbackCountry = await _db.Countries
            .AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.CountryPhoneCode })
            .FirstOrDefaultAsync(ct);

        if (fallbackCountry is null)
            return ServiceResult<object>.Fail("Aucun pays actif n'est configuré dans le système.");

        // 2. Mapping vers le DTO métier
        var mapped = new CompanyCreateDto
        {
            CompanyName = request.CompanyName.Trim(),
            CompanyEmail = request.CompanyEmail.Trim(),
            CompanyPhoneNumber = request.CompanyPhoneNumber.Trim(),
            CompanyAddress = "Adresse à compléter",
            CityName = "Ville à compléter",
            CnssNumber = $"TEMP-CNSS-{Guid.NewGuid():N}"[..22],
            CountryId = fallbackCountry.Id,
            CountryPhoneCode = fallbackCountry.CountryPhoneCode,
            IsCabinetExpert = false,
            AdminFirstName = request.AdminFirstName.Trim(),
            AdminLastName = request.AdminLastName.Trim(),
            AdminEmail = request.AdminEmail.Trim(),
            AdminPhone = request.AdminPhone.Trim(),
            isActive = true
        };

        // 3. Validation FluentValidation
        var validation = await _createValidator.ValidateAsync(mapped, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                .ToList();
            return ServiceResult<object>.Fail(errors);
        }

        // 4. Création (system user MVP = 1)
        const int systemUserId = 1;
        var result = await _companySvc.CreateAsync(mapped, systemUserId, ct);

        return result.Success
            ? ServiceResult<object>.Ok(result.Data!)
            : ServiceResult<object>.Fail(result.Error ?? "Erreur lors de la création.");
    }

    public async Task<ServiceResult<object>> CompleteCompanyOnboardingAsync(
        AuthenticatedCompanySignupDto request,
        int currentUserId,
        string currentUserEmail,
        CancellationToken ct)
    {
        var currentUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId && u.DeletedAt == null, ct);
        if (currentUser is null)
            return ServiceResult<object>.Fail("Utilisateur introuvable.");

        if (currentUser.EmployeeId.HasValue)
            return ServiceResult<object>.Fail("Ce compte est déjà rattaché à une entreprise.");

        var fallbackCountry = await _db.Countries
            .AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.CountryPhoneCode })
            .FirstOrDefaultAsync(ct);

        if (fallbackCountry is null)
            return ServiceResult<object>.Fail("Aucun pays actif n'est configuré dans le système.");

        var mapped = new CompanyCreateDto
        {
            CompanyName = request.CompanyName.Trim(),
            CompanyEmail = string.IsNullOrWhiteSpace(request.CompanyEmail)
                ? currentUserEmail.Trim()
                : request.CompanyEmail.Trim(),
            CompanyPhoneNumber = string.IsNullOrWhiteSpace(request.CompanyPhoneNumber)
                ? request.AdminPhone.Trim()
                : request.CompanyPhoneNumber.Trim(),
            CompanyAddress = "Adresse à compléter",
            CountryId = fallbackCountry.Id,
            CityName = "Ville à compléter",
            CnssNumber = $"TEMP-CNSS-{Guid.NewGuid():N}"[..22],
            CountryPhoneCode = fallbackCountry.CountryPhoneCode,
            IsCabinetExpert = false,
            AdminFirstName = request.AdminFirstName.Trim(),
            AdminLastName = request.AdminLastName.Trim(),
            AdminEmail = currentUserEmail.Trim(),
            AdminPhone = request.AdminPhone.Trim(),
            isActive = true
        };

        var validation = await _createValidator.ValidateAsync(mapped, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .Select(g => $"{g.Key}: {string.Join(" | ", g.Select(x => x.ErrorMessage))}")
                .ToList();

            return ServiceResult<object>.Fail(errors);
        }

        var result = await _companySvc.CreateAsync(
            mapped,
            createdBy: currentUserId,
            ct: ct,
            sendInvitation: false,
            existingAdminUserId: currentUserId);

        if (!result.Success)
            return ServiceResult<object>.Fail(result.Error ?? "Erreur lors de la création.");

        var createdEmployeeId = result.Data?.Admin?.EmployeeId;
        if (createdEmployeeId is not > 0)
            return ServiceResult<object>.Fail("Entreprise créée mais liaison admin incomplète.");

        currentUser.EmployeeId = createdEmployeeId.Value;
        currentUser.Source = "entra";
        currentUser.IsActive = true;

        var adminRole = await _db.Roles
            .FirstOrDefaultAsync(x => x.Name.ToLower() == "admin" && x.DeletedAt == null, ct);
        if (adminRole is null)
            return ServiceResult<object>.Fail("Rôle Admin introuvable.");

        var existingRole = await _db.UsersRoles
            .FirstOrDefaultAsync(ur => ur.UserId == currentUser.Id && ur.RoleId == adminRole.Id && ur.DeletedAt == null, ct);
        if (existingRole is null)
        {
            _db.UsersRoles.Add(new UsersRoles
            {
                UserId = currentUser.Id,
                RoleId = adminRole.Id,
                CreatedBy = currentUser.Id
            });
        }

        await _db.SaveChangesAsync(ct);

        return ServiceResult<object>.Ok(result.Data!);
    }
}