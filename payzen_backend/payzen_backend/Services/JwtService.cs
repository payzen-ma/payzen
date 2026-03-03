using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using payzen_backend.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace payzen_backend.Services
{
    public class JwtService
    {
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expires;
        private readonly AppDbContext _db;

        public JwtService(IConfiguration config, AppDbContext db)
        {
            _key = config["JwtSettings:Key"] 
                ?? throw new InvalidOperationException("JWT Key not configured");
            _issuer = config["JwtSettings:Issuer"] 
                ?? throw new InvalidOperationException("JWT Issuer not configured");
            _audience = config["JwtSettings:Audience"] 
                ?? throw new InvalidOperationException("JWT Audience not configured");
            _expires = int.Parse(config["JwtSettings:ExpiresInMinutes"] ?? "120");
            _db = db;
        }

        /// <summary>
        /// Génère un token JWT pour un utilisateur avec ses permissions
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="email">Email de l'utilisateur</param>
        /// <returns>Token JWT signé avec les permissions</returns>
        public async Task<string> GenerateTokenAsync(int userId, string email)
        {
            // Récupérer les permissions de l'utilisateur via ses rôles
            var permissions = await _db.UsersRoles
                .Where(ur => ur.UserId == userId && ur.DeletedAt == null)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.DeletedAt == null)
                .SelectMany(ur => _db.RolesPermissions
                    .Where(rp => rp.RoleId == ur.RoleId && rp.DeletedAt == null)
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.Permission.DeletedAt == null)
                    .Select(rp => rp.Permission.Name))
                .Distinct()
                .ToListAsync();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, email),
                new Claim("uid", userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Ajouter chaque permission comme un claim "permission"
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expires),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
