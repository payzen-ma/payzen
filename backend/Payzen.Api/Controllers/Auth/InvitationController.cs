using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Auth;
using Payzen.Application.Interfaces;
using System.Security.Claims;

namespace Payzen.Api.Controllers.Auth;

[ApiController]
[Route("api/invitations")]
public class InvitationController : ControllerBase
{
    private readonly IInvitationService _invitationService;

    public InvitationController(IInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    /// <summary>
    /// Valide un token d'invitation et retourne les infos (company, rôle, email masqué)
    /// </summary>
    [HttpGet("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate([FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { Message = "Token manquant." });
        
        var info = await _invitationService.ValidateTokenAsync(token, ct);
        
        return info != null 
            ? Ok(info) 
            : NotFound(new { Message = "Invitation invalide ou expirée." });
    }

    /// <summary>
    /// Accepte une invitation après authentification via IdP (Microsoft ou Google)
    /// L'email est extrait du JWT Entra (claim "email" ou "preferred_username")
    /// </summary>
    [HttpPost("accept-via-idp")]
    [Authorize]
    public async Task<IActionResult> AcceptViaIdp(
        [FromBody] AcceptInvitationViaIdpDto dto, 
        CancellationToken ct)
    {
        // L'email vient du claim "email" du JWT Entra — jamais du body
        var idpEmail = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue("preferred_username")
                    ?? User.FindFirstValue("email");
        
        if (string.IsNullOrEmpty(idpEmail))
            return Unauthorized(new { Message = "Impossible de récupérer l'email depuis l'IdP." });
        
        var result = await _invitationService.AcceptViaIdpAsync(dto.Token, idpEmail, ct);
        
        if (result.IsSuccess)
            return Ok(new { Message = "Invitation acceptée avec succès." });
        
        return result.Error switch
        {
            "EMAIL_MISMATCH" => BadRequest(new
            {
                Message = $"L'email de votre compte ({result.ReceivedEmail}) ne correspond pas " +
                          $"à l'invitation ({MaskEmail(result.ExpectedEmail!)}).",
                Code = "EMAIL_MISMATCH",
                ExpectedEmail = MaskEmail(result.ExpectedEmail!),
                ReceivedEmail = result.ReceivedEmail
            }),
            "INVITATION_EXPIRED" => BadRequest(new 
            { 
                Message = "Cette invitation a expiré.", 
                Code = result.Error 
            }),
            "INVITATION_ALREADY_USED" => BadRequest(new 
            { 
                Message = "Cette invitation a déjà été utilisée.", 
                Code = result.Error 
            }),
            _ => NotFound(new 
            { 
                Message = "Invitation introuvable.", 
                Code = result.Error 
            })
        };
    }

    /// <summary>
    /// Créer une invitation pour un admin company
    /// </summary>
    [HttpPost("invite-admin")]
    //[Authorize(Roles = "Admin,Admin Payzen")]
    public async Task<IActionResult> InviteAdmin(
        [FromBody] InviteAdminDto dto, 
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var token = await _invitationService.CreateInvitationAsync(dto, ct);
        
        return Ok(new 
        { 
            Message = "Invitation envoyée avec succès.", 
            Token = token 
        });
    }

    /// <summary>
    /// Créer une invitation pour un employé
    /// </summary>
    [HttpPost("invite-employee")]
    [Authorize(Roles = "Admin,RH,AdminPayzen")]
    public async Task<IActionResult> InviteEmployee(
        [FromBody] InviteEmployeeDto dto, 
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var token = await _invitationService.CreateEmployeeInvitationAsync(dto, ct);
        
        return Ok(new 
        { 
            Message = "Invitation envoyée avec succès.", 
            Token = token 
        });
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***" + email[at..];
        return email[0] + "***" + email[(at - 1)..];
    }
}