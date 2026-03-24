using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Auth;
using Payzen.Application.Interfaces;

namespace Payzen.Api.Controllers.Auth;

[ApiController]
[Route("api/auth/native")]
[AllowAnonymous]
public class NativeAuthController : ControllerBase
{
    private readonly INativeAuthService _native;
    private readonly IAuthService _auth;

    public NativeAuthController(INativeAuthService native, IAuthService auth)
    {
        _native = native;
        _auth   = auth;
    }

    /// <summary>
    /// Connexion email + mot de passe via Entra Native Auth.
    /// Retourne un JWT Payzen identique au flow existant.
    /// </summary>
    [HttpPost("signin")]
    public async Task<ActionResult> SignIn(
        [FromBody] NativeSignInDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // 1. Valider les credentials auprès d'Entra
        var signInResult = await _native.SignInAsync(dto.Email, dto.Password, ct);
        if (!signInResult.Success)
            return Unauthorized(new { Message = signInResult.Error });

        var (email, oid) = signInResult.Data;

        // 2. Créer/récupérer le user Payzen et générer le JWT
        // LoginWithEntraAsync est inchangé — même logique qu'aujourd'hui
        var loginResult = await _auth.LoginWithEntraAsync(
            new EntraLoginRequestDto { Email = email, ExternalId = oid }, ct);

        return loginResult.Success
            ? Ok(loginResult.Data)
            : Unauthorized(new { Message = loginResult.Error });
    }

    /// <summary>
    /// Création de compte email + mot de passe.
    /// Entra envoie automatiquement l'email de vérification.
    /// </summary>
    [HttpPost("signup")]
    public async Task<ActionResult> SignUp(
        [FromBody] NativeSignUpDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _native.SignUpAsync(dto.Email, dto.Password, ct);
        if (!result.Success)
            return BadRequest(new { Message = result.Error });

        return Ok(new
        {
            Message = "Compte créé. Vérifiez votre email pour activer votre compte."
        });
    }
}