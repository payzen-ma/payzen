using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Company;
using Payzen.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Payzen.Infrastructure.Persistence;
using System.Linq;

namespace Payzen.Api.Controllers.Public;

[Serializable]
public class PublicCompanySignupDto
{
    public required string CompanyName { get; set; }
    public required string CompanyEmail { get; set; }
    public required string CompanyPhoneNumber { get; set; }
    public required string AdminFirstName { get; set; }
    public required string AdminLastName { get; set; }
    public required string AdminEmail { get; set; }
    public required string AdminPhone { get; set; }
}

[ApiController]
[Route("api/public")]
public class PublicSignupController : ControllerBase
{
    private readonly ICompanyService _svc;
    private readonly IValidator<CompanyCreateDto> _createValidator;
    private readonly AppDbContext _db;

    public PublicSignupController(
        ICompanyService svc,
        IValidator<CompanyCreateDto> createValidator,
        AppDbContext db)
    {
        _svc = svc;
        _createValidator = createValidator;
        _db = db;
    }

    /// <summary>
    /// Signup public (landing) : crée une company + un admin (fiche employé) puis envoie l'invitation.
    /// Le user final active son compte via Microsoft Entra External ID (0 password policy).
    /// </summary>
    [HttpPost("signup-company-admin")]
    [AllowAnonymous]
    public async Task<ActionResult> SignupCompanyAdmin([FromBody] PublicCompanySignupDto dto, CancellationToken ct)
    {
        // Champs minimaux demandés par le front "public signup"
        if (string.IsNullOrWhiteSpace(dto.CompanyName) ||
            string.IsNullOrWhiteSpace(dto.CompanyEmail) ||
            string.IsNullOrWhiteSpace(dto.CompanyPhoneNumber) ||
            string.IsNullOrWhiteSpace(dto.AdminFirstName) ||
            string.IsNullOrWhiteSpace(dto.AdminLastName) ||
            string.IsNullOrWhiteSpace(dto.AdminEmail) ||
            string.IsNullOrWhiteSpace(dto.AdminPhone))
        {
            return BadRequest(new { Message = "Tous les champs obligatoires du formulaire sont requis." });
        }

        var fallbackCountry = await _db.Countries
            .AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.CountryPhoneCode })
            .FirstOrDefaultAsync(ct);

        if (fallbackCountry == null)
            return BadRequest(new { Message = "Aucun pays actif n'est configuré dans le système." });

        // Mapping vers le DTO métier complet avec valeurs techniques par défaut.
        var mapped = new CompanyCreateDto
        {
            CompanyName = dto.CompanyName.Trim(),
            CompanyEmail = dto.CompanyEmail.Trim(),
            CompanyPhoneNumber = dto.CompanyPhoneNumber.Trim(),
            CompanyAddress = "Adresse à compléter",
            CountryId = fallbackCountry.Id,
            CityName = "Ville à compléter",
            CnssNumber = $"TEMP-CNSS-{Guid.NewGuid():N}"[..22],
            CountryPhoneCode = fallbackCountry.CountryPhoneCode,
            IsCabinetExpert = false,
            AdminFirstName = dto.AdminFirstName.Trim(),
            AdminLastName = dto.AdminLastName.Trim(),
            AdminEmail = dto.AdminEmail.Trim(),
            AdminPhone = dto.AdminPhone.Trim(),
            isActive = true
        };

        var validation = await _createValidator.ValidateAsync(mapped, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                Message = "Données invalides.",
                Errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            });
        }

        // MVP: créé par un "system user" fictif (admin + onboarding + invitation).
        var createdBy = 1;

        var r = await _svc.CreateAsync(mapped, createdBy, ct);
        if (!r.Success)
        {
            if (r.Error?.Contains("existe déjà") == true)
                return Conflict(new { Message = r.Error });

            return BadRequest(new { Message = r.Error });
        }

        return Ok(r.Data);
    }
}

