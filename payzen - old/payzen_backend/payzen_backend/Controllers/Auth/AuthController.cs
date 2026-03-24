using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Auth;
using payzen_backend.Services;

namespace payzen_backend.Controllers.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly JwtService _jwt;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, JwtService jwt, IConfiguration config)
        {
            _db = db;
            _jwt = jwt;
            _config = config;
        }

        /// <summary>
        /// Authentifie un utilisateur avec son email et mot de passe
        /// </summary>
        /// <param name="loginRequest">Données de connexion (email et mot de passe)</param>
        /// <returns>Token JWT et informations utilisateur</returns>
        /// <response code="200">Authentification réussie, retourne le token JWT</response>
        /// <response code="401">Email ou mot de passe incorrect</response>
        /// <response code="400">Données de connexion invalides</response>
        /// Exemple de requête :
        /// 
        ///     POST /api/auth/login
        ///     {
        ///         "firstName": "Mohammed",
        ///         "lastName": "Shab",
        ///         "cinNumber": "AB123456",
        ///         "dateOfBirth": "1990-01-01",
        ///         "phoneNumber": "34567890",
        ///         "email": "mo.shab@gmail.com",
        ///         "companyId": 1,
        ///         "createUserAccount": true,
        ///     }
        [HttpPost("login")]
        [Produces("application/json")] // Retourne du JSON
        // Retiré [Consumes] pour être plus tolérant
        public async Task<ActionResult> Login([FromBody] LoginRequest loginRequest)
        {            
            // Validation du modèle
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"   - {error.ErrorMessage}");
                }
                return BadRequest(new 
                { 
                    Message = "Données invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            if (loginRequest == null)
                return BadRequest(new { Message = "Les données de connexion sont requises" });

            // Recherche de l'utilisateur avec ses relations Employee
            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Employee) // Inclure l'employé si lié
                .Where(u => u.Email == loginRequest.Email
                        && u.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (user == null)
                return Unauthorized(new { Message = "Email ou mot de passe incorrect" });

            var passwordValid = user.VerifyPassword(loginRequest.Password);

            if (!passwordValid)
                return Unauthorized(new { Message = "Email ou mot de passe incorrect" });

            // Vérifier si l'utilisateur est actif
            if (!user.IsActive)
                return Unauthorized(new { Message = "Votre compte est désactivé. Veuillez contacter l'administrateur." });

            // Vérifier la company est active si l'utilisateur est lié à une company via employee
            if (user.Employee != null)
            {
                var companyExist = await _db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == user.Employee.CompanyId && c.DeletedAt == null);
                if (companyExist == null || !companyExist.isActive)
                {
                    return Unauthorized(new { Message = "Votre entreprise est désactivée. Veuillez contacter l'administrateur." });
                }
            }
            // Récupérer les rôles de l'utilisateur
            var userRoles = await _db.UsersRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == user.Id && ur.DeletedAt == null)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            // Récupérer toutes les permissions de l'utilisateur via ses rôles
            var userPermissions = await _db.RolesPermissions
                .AsNoTracking()
                .Where(rp => _db.UsersRoles
                    .Where(ur => ur.UserId == user.Id && ur.DeletedAt == null)
                    .Select(ur => ur.RoleId)
                    .Contains(rp.RoleId) && rp.DeletedAt == null)
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            // Récupération de l'employee lié
            var employee = user.Employee;

            // Récupérer la société liée via l'employé, si applicable
            var company = employee != null
                ? await _db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == employee.CompanyId && c.DeletedAt == null)
                : null;
            var isCabinetExpert = company != null ? company.IsCabinetExpert : (bool?)null;

            Console.WriteLine("==========================================================");
            Console.WriteLine("==========================================================");
            Console.WriteLine("==========================================================");
            Console.WriteLine($" IsCabinetExpert: {isCabinetExpert}");
            Console.WriteLine($"Company ID is : {company?.Id}");

            // Génération du token JWT
            var token = await _jwt.GenerateTokenAsync(user.Id, user.Email);

            var expiresInMinutes = int.Parse(_config["JwtSettings:ExpiresInMinutes"] ?? "600");
            var expiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes);
            
            // Get Categories and mode
            var category = employee != null
                ? await _db.EmployeeCategories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ec => ec.Id == employee.CategoryId && ec.DeletedAt == null)
                : null;
            var mode = category != null ? category.Mode.ToString() : null;
            // Créer la réponse avec toutes les informations
            var response = new LoginResponse
            {
                Message = "Authentification réussie",
                Token = token,
                ExpiresAt = expiresAt,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    FirstName = user.Employee?.FirstName ?? "",
                    LastName = user.Employee?.LastName ?? "",
                    Roles = userRoles,
                    Permissions = userPermissions,
                    EmployeeId = user.Employee?.Id,
                    EmployeeCategoryId = user.Employee?.CategoryId,
                    Mode = mode,
                    isCabinetExpert = isCabinetExpert ?? false,
                    companyId = company?.Id ?? 0,
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Déconnexion (côté client)
        /// </summary>
        [HttpPost("logout")]
        [Produces("application/json")]
        public IActionResult Logout()
        {
            return Ok(new { Message = "Déconnexion réussie. Veuillez supprimer le token côté client." });
        }

        /// <summary>
        /// Route /me pour obtenir les informations de l'utilisateur actuel
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [Produces("application/json")]
        public async Task<IActionResult> GetMe()
        {
            // Récupérer le userId depuis les claims du token JWT
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
            
            if (userIdClaim == null)
            {
                return Unauthorized(new { Message = "Utilisateur non authentifié" });
            }

            if (!int.TryParse(userIdClaim.Value, out var userId))
            {
                return BadRequest(new { Message = "ID utilisateur invalide" });
            }

            // Récupérer l'utilisateur avec ses relations
            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.DeletedAt == null);

            if (user == null)
            {
                return NotFound(new { Message = "Utilisateur non trouvé" });
            }

            // Récupérer les rôles de l'utilisateur
            var userRoles = await _db.UsersRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == user.Id && ur.DeletedAt == null)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.DeletedAt == null)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            // Récupérer les permissions de l'utilisateur via ses rôles
            var userPermissions = await _db.RolesPermissions
                .AsNoTracking()
                .Where(rp => _db.UsersRoles
                    .Where(ur => ur.UserId == user.Id && ur.DeletedAt == null)
                    .Select(ur => ur.RoleId)
                    .Contains(rp.RoleId) && rp.DeletedAt == null)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Permission.DeletedAt == null)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                FirstName = user.Employee?.FirstName ?? "",
                LastName = user.Employee?.LastName ?? "",
                Roles = userRoles,
                Permissions = userPermissions
            };

            return Ok(userInfo);
        }
    }
}
