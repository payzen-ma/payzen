using Payzen.Application.DTOs.Company;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Api.Extensions;
using Payzen.Application.Interfaces;
using Payzen.Application.Common;

namespace Payzen.Api.Controllers.Public;


[ApiController]
[Route("api/public")]
public class PublicSignupController : ControllerBase
{
    private readonly IPublicSignupService _signup;

    public PublicSignupController(
        IPublicSignupService signup)
    {
        _signup = signup;
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

        var r = await _signup.SignupCompanyAdminAsync(
            new PublicSignupRequest(
                dto.CompanyName.Trim(),
                dto.CompanyEmail.Trim(),
                dto.CompanyPhoneNumber.Trim(),
                dto.AdminFirstName.Trim(),
                dto.AdminLastName.Trim(),
                dto.AdminEmail.Trim(),
                dto.AdminPhone.Trim()),
            ct);

        if (!r.Success)
        {
            if (r.Error?.Contains("existe déjà") == true)
                return Conflict(new { Message = r.Error });

            return BadRequest(new { Message = r.Error });
        }

        return Ok(r.Data);
    }

    /// <summary>
    /// Signup post-authentification Entra : complète les informations société après vérification d'email.
    /// Le compte utilisateur courant devient admin de la société, sans flux d'invitation.
    /// </summary>
    [HttpPost("complete-company-onboarding")]
    [Authorize]
    public async Task<ActionResult> CompleteCompanyOnboarding([FromBody] AuthenticatedCompanySignupDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.CompanyName) ||
            string.IsNullOrWhiteSpace(dto.AdminFirstName) ||
            string.IsNullOrWhiteSpace(dto.AdminLastName) ||
            string.IsNullOrWhiteSpace(dto.AdminPhone))
        {
            return BadRequest(new { Message = "Tous les champs obligatoires du formulaire sont requis." });
        }

        var currentUserId = User.GetUserId();
        var currentUserEmail = User.GetUserEmail();

        var r = await _signup.CompleteCompanyOnboardingAsync(dto, currentUserId, currentUserEmail, ct);
        if (!r.Success)
        {
            if (r.Error?.Contains("déjà") == true || r.Error?.Contains("rattach") == true)
                return Conflict(new { Message = r.Error });

            return BadRequest(new { Message = r.Error });
        }

        return Ok(r.Data);
    }
}

