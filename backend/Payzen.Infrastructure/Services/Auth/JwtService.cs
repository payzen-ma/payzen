using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Auth;
using Payzen.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Payzen.Infrastructure.Services.Auth;

public class JwtService : IJwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiresMinutes;
    private readonly AppDbContext _db;

    public JwtService(IConfiguration config, AppDbContext db)
    {
        _key = config["JwtSettings:Key"] ?? throw new InvalidOperationException("JwtSettings:Key manquant");
        if (string.IsNullOrWhiteSpace(_key))
            throw new InvalidOperationException("JwtSettings:Key vide. Fournissez une clé JWT (>= 32 caractères).");
        if (_key.Length < 32)
            throw new InvalidOperationException("JwtSettings:Key trop courte. Utilisez au moins 32 caractères.");
        _issuer = config["JwtSettings:Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer manquant");
        _audience = config["JwtSettings:Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience manquant");
        _expiresMinutes = int.Parse(config["JwtSettings:ExpiresInMinutes"] ?? "120");
        _db = db;
    }

    public async Task<string> GenerateTokenAsync(Users user, CancellationToken ct = default)
    {
        // Récupère les rôles actifs de l'utilisateur
        var roles = await _db.UsersRoles
            .Where(ur => ur.UserId == user.Id && ur.DeletedAt == null)
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.DeletedAt == null)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToListAsync(ct);

        // Récupère les permissions via les rôles
        var roleIds = await _db.UsersRoles
            .Where(ur => ur.UserId == user.Id && ur.DeletedAt == null)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        var permissions = await _db.RolesPermissions
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.DeletedAt == null)
            .Include(rp => rp.Permission)
            .Where(rp => rp.Permission.DeletedAt == null)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync(ct);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,        user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email,       user.Email),
            new(JwtRegisteredClaimNames.UniqueName,  user.Username),
            new(JwtRegisteredClaimNames.Jti,         Guid.NewGuid().ToString()),
            new("uid",                               user.Id.ToString()),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, parameters, out _);
            var uidClaim = principal.FindFirst("uid") ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);

            return uidClaim != null && int.TryParse(uidClaim.Value, out var userId)
                ? userId
                : null;
        }
        catch
        {
            return null;
        }
    }
}
