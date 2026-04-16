using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Auth;
using Payzen.Application.Interfaces;

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

        return info != null ? Ok(info) : NotFound(new { Message = "Invitation invalide ou expirée." });
    }

    /// <summary>
    /// Accepte une invitation après authentification (JWT PayZen, obtenu après Entra côté SPA).
    /// L’utilisateur est résolu par <c>uid</c>/<c>sub</c> (fiable) puis par e-mail d’invitation ; pas uniquement par les claims e-mail du JWT.
    /// </summary>
    [HttpPost("accept-via-idp")]
    [Authorize]
    public async Task<IActionResult> AcceptViaIdp([FromBody] AcceptInvitationViaIdpDto dto, CancellationToken ct)
    {
        var uidStr = User.FindFirst("uid")?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        int? jwtUserId = int.TryParse(uidStr, out var parsedId) ? parsedId : null;

        var result = await _invitationService.AcceptViaIdpAsync(dto.Token, jwtUserId, ct);

        if (result.IsSuccess)
            return Ok(new { Message = "Invitation acceptée avec succès." });

        return result.Error switch
        {
            "EMAIL_MISMATCH" => BadRequest(
                new
                {
                    Message = $"L'email de votre compte ({result.ReceivedEmail}) ne correspond pas "
                        + $"à l'invitation ({MaskEmail(result.ExpectedEmail!)}).",
                    Code = "EMAIL_MISMATCH",
                    ExpectedEmail = MaskEmail(result.ExpectedEmail!),
                    ReceivedEmail = result.ReceivedEmail,
                }
            ),
            "INVITATION_EXPIRED" => BadRequest(new { Message = "Cette invitation a expiré.", Code = result.Error }),
            "INVITATION_ALREADY_USED" => BadRequest(
                new { Message = "Cette invitation a déjà été utilisée.", Code = result.Error }
            ),
            "USER_NOT_LINKED" => BadRequest(
                new
                {
                    Message = "Compte introuvable pour finaliser l’invitation. Réessayez après connexion ou contactez le support.",
                    Code = result.Error,
                }
            ),
            "MISSING_ACTIVE_STATUS" => StatusCode(
                500,
                new
                {
                    Message = "Configuration serveur incomplète : statut employé « Active » introuvable.",
                    Code = result.Error,
                }
            ),
            _ => NotFound(new { Message = "Invitation introuvable.", Code = result.Error }),
        };
    }

    /// <summary>
    /// Créer une invitation pour un admin company
    /// </summary>
    [HttpPost("invite-admin")]
    //[Authorize(Roles = "Admin Payzen")]
    public async Task<IActionResult> InviteAdmin([FromBody] InviteAdminDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var token = await _invitationService.CreateInvitationAsync(dto, ct);

        return Ok(new { Message = "Invitation envoyée avec succès.", Token = token });
    }

    /// <summary>
    /// Créer une invitation pour un employé
    /// </summary>
    [HttpPost("invite-employee")]
    [Authorize(Roles = "Admin,RH,Admin Payzen")]
    public async Task<IActionResult> InviteEmployee([FromBody] InviteEmployeeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var token = await _invitationService.CreateEmployeeInvitationAsync(dto, ct);

        return Ok(new { Message = "Invitation envoyée avec succès.", Token = token });
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
            return "***" + email[at..];
        return email[0] + "***" + email[(at - 1)..];
    }
}
