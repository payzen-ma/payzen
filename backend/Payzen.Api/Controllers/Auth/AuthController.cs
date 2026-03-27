using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Auth;
using Payzen.Application.Interfaces;

namespace Payzen.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [Produces("application/json")]
    public async Task<ActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);
        
        var result = await _auth.LoginAsync(dto);
        
        return result.Success ? Ok(result.Data) : Unauthorized(new { Message = result.Error });
    }

    [HttpPost("entra-login")]
    [AllowAnonymous]
    [Produces("application/json")]
    public async Task<ActionResult> EntraLogin([FromBody] EntraLoginRequestDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);
        
        var result = await _auth.LoginWithEntraAsync(dto, ct);
        
        return result.Success ? Ok(result.Data) : Unauthorized(new { Message = result.Error });
    }

    [HttpGet("me")]
    [Authorize]
    [Produces("application/json")]
    public async Task<ActionResult> Me()
    {
        var userId = User.FindFirst("uid")?.Value;
        
        if (userId == null) 
            return Unauthorized(new {Message = "Utilisateur non authentifi�"});
        
        var result = await _auth.GetMeAsync(int.Parse(userId));
        
        return result.Success ? Ok(result.Data) : NotFound(new { result.Error });
    }

    [HttpPatch("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirst("uid")?.Value;
       
        if (userId == null) 
            return Unauthorized();
        
        var result = await _auth.ChangePasswordAsync(int.Parse(userId), dto);
        
        return result.Success ? Ok() : BadRequest(new { result.Error });
    }

    [HttpPost("logout")]
    [Produces("application/json")]
    public ActionResult Logout()
    {
        return Ok(new { Message = "Déconnexion réussie." });
    }
}
