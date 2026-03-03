using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Permissions;
using payzen_backend.Models.Permissions.Dtos;
using payzen_backend.Extensions;

namespace payzen_backend.Controllers.Auth
{
    [Route("api/permissions")]
    [ApiController]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PermissionsController(AppDbContext db) => _db = db;

        /// <summary>
        /// Récupère toutes les permissions actives
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PermissionReadDto>>> GetAll()
        {
            var permissions = await _db.Permissions
                .AsNoTracking()
                .Where(p => p.DeletedAt == null)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var result = permissions.Select(p => new PermissionReadDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Resource = p.Resource,
                Action = p.Action,
                CreatedAt = p.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Récupère une permission par ID
        /// </summary>
        [HttpGet("{id}", Name = "GetPermissionById")]
        public async Task<ActionResult<PermissionReadDto>> GetById(int id)
        {
            var permission = await _db.Permissions
                .AsNoTracking()
                .Where(p => p.DeletedAt == null)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permission == null)
                return NotFound(new { Message = "Permission non trouvée" });

            var result = new PermissionReadDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                Resource = permission.Resource,
                CreatedAt = permission.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Crée une nouvelle permission
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PermissionReadDto>> Create([FromBody] PermissionCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            if (await _db.Permissions.AnyAsync(p => p.Name == dto.Name && p.DeletedAt == null))
            {
                return Conflict(new { Message = "Une permission avec ce nom existe déjà" });
            }

            var permission = new Permissions
            {
                Name = dto.Name,
                Description = dto.Description,
                Resource = dto.Resource,
                Action = dto.Action,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.Permissions.Add(permission);
            await _db.SaveChangesAsync();

            var readDto = new PermissionReadDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                Resource = permission.Resource,
                Action = permission.Action,
                CreatedAt = permission.CreatedAt.DateTime
            };

            return CreatedAtRoute("GetPermissionById", new { id = permission.Id }, readDto);
        }

        /// <summary>
        /// Met à jour une permission
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PermissionReadDto>> Update(int id, [FromBody] PermissionUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            var permission = await _db.Permissions
                .Where(p => p.Id == id && p.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (permission == null)
                return NotFound(new { Message = "Permission non trouvée" });

            if (dto.Name != null && dto.Name != permission.Name)
            {
                if (await _db.Permissions.AnyAsync(p => p.Name == dto.Name && p.Id != id && p.DeletedAt == null))
                {
                    return Conflict(new { Message = "Une permission avec ce nom existe déjà" });
                }
                permission.Name = dto.Name;
            }

            if (dto.Description != null)
            {
                permission.Description = dto.Description;
            }

            permission.UpdatedAt = DateTimeOffset.UtcNow;
            permission.UpdatedBy = userId;

            await _db.SaveChangesAsync();

            var readDto = new PermissionReadDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                CreatedAt = permission.CreatedAt.DateTime
            };

            return Ok(readDto);
        }

        /// <summary>
        /// Supprime une permission (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();

            var permission = await _db.Permissions
                .Where(p => p.Id == id && p.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (permission == null)
                return NotFound(new { Message = "Permission non trouvée" });

            // Vérifier si la permission est assignée à des rôles
            var isAssigned = await _db.RolesPermissions
                .AnyAsync(rp => rp.PermissionId == id && rp.DeletedAt == null);

            if (isAssigned)
            {
                return BadRequest(new { Message = "Impossible de supprimer cette permission car elle est assignée à des rôles" });
            }

            permission.DeletedAt = DateTimeOffset.UtcNow;
            permission.DeletedBy = userId;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Calculer le nombre total de permissions actives
        /// </summary>
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCount()
        {
            var count = await _db.Permissions
                .AsNoTracking()
                .Where(p => p.DeletedAt == null)
                .CountAsync();
            return Ok(count);
        }
    }
}
