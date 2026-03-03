using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Authorization;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Users;
using payzen_backend.Models.Users.Dtos;

namespace payzen_backend.Controllers.Auth
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UsersController(AppDbContext db) => _db = db;

        /// <summary>
        /// Récupère tous les utilisateurs actifs
        /// </summary>
        [HttpGet]
        //[HasPermission("READ_USERS")]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> GetAll()
        {
            var users = await _db.Users
                .AsNoTracking()
                .Where(u => u.DeletedAt == null)
                .OrderBy(u => u.Username)
                .ToListAsync();

            var result = users.Select(u => new UserReadDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Récupère un utilisateur par ID
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission("VIEW_USERS")]
        public async Task<ActionResult<UserReadDto>> GetById(int id)
        {
            var user = await _db.Users
                .AsNoTracking()
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new { Message = "Utilisateur non trouvé" });

            var result = new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Crée un nouvel utilisateur
        /// </summary>
        [HttpPost]
        //[HasPermission("CREATE_USERS")]
        public async Task<ActionResult<UserReadDto>> Create([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // Vérifier que l'email n'existe pas déjà
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.DeletedAt == null))
            {
                return Conflict(new { Message = "Un utilisateur avec cet email existe déjà" });
            }

            // Vérifier que le username n'existe pas déjà
            if (await _db.Users.AnyAsync(u => u.Username == dto.Username && u.DeletedAt == null))
            {
                return Conflict(new { Message = "Un utilisateur avec ce nom d'utilisateur existe déjà" });
            }

            var user = new Users
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsActive = dto.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var readDto = new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt.DateTime
            };

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, readDto);
        }

        /// <summary>
        /// Met à jour un utilisateur
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission("EDIT_USERS")]
        public async Task<ActionResult<UserReadDto>> Update(int id, [FromBody] UserUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            var user = await _db.Users
                .Where(u => u.Id == id && u.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { Message = "Utilisateur non trouvé" });

            // Mettre à jour les champs si fournis
            if (dto.Email != null && dto.Email != user.Email)
            {
                if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id && u.DeletedAt == null))
                {
                    return Conflict(new { Message = "Un utilisateur avec cet email existe déjà" });
                }
                user.Email = dto.Email;
            }

            if (dto.Username != null && dto.Username != user.Username)
            {
                if (await _db.Users.AnyAsync(u => u.Username == dto.Username && u.Id != id && u.DeletedAt == null))
                {
                    return Conflict(new { Message = "Un utilisateur avec ce nom d'utilisateur existe déjà" });
                }
                user.Username = dto.Username;
            }

            if (dto.Password != null)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            if (dto.IsActive.HasValue)
            {
                user.IsActive = dto.IsActive.Value;
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.UpdatedBy = userId;

            await _db.SaveChangesAsync();

            var readDto = new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt.DateTime
            };

            return Ok(readDto);
        }

        /// <summary>
        /// Supprime un utilisateur (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission("DELETE_USERS")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();

            var user = await _db.Users
                .Where(u => u.Id == id && u.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { Message = "Utilisateur non trouvé" });

            // Empêcher l'auto-suppression
            if (user.Id == userId)
            {
                return BadRequest(new { Message = "Vous ne pouvez pas supprimer votre propre compte" });
            }

            // Vérifier si l'utilisateur a des rôles assignés
            var hasRoles = await _db.UsersRoles
                .AnyAsync(ur => ur.UserId == id && ur.DeletedAt == null);

            if (hasRoles)
            {
                return BadRequest(new { Message = "Impossible de supprimer cet utilisateur car il a des rôles assignés. Veuillez d'abord retirer ses rôles." });
            }

            user.DeletedAt = DateTimeOffset.UtcNow;
            user.DeletedBy = userId;

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
