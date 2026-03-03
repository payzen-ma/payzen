using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Permissions;
using payzen_backend.Models.Permissions.Dtos;
using payzen_backend.Extensions;

namespace payzen_backend.Controllers.Auth
{
    [Route("api/roles-permissions")]
    [ApiController]
    [Authorize]
    public class RolesPermissionsController : ControllerBase
    {
        private readonly AppDbContext _db;
        
        public RolesPermissionsController(AppDbContext db) => _db = db;

        /// <summary>
        /// Récupère toutes les permissions d'un rôle
        /// </summary>
        /// <param name="roleId">ID du rôle</param>
        /// <returns>Liste des permissions du rôle</returns>
        [HttpGet("role/{roleId}")]
        public async Task<ActionResult<IEnumerable<PermissionReadDto>>> GetRolePermissions(int roleId)
        {
            // Vérifier que le rôle existe et est actif
            var roleExists = await _db.Roles
                .AnyAsync(r => r.Id == roleId && r.DeletedAt == null);
            
            if (!roleExists)
                return NotFound(new { Message = "Rôle non trouvé" });

            // Récupérer les permissions du rôle
            var permissions = await _db.RolesPermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId && rp.DeletedAt == null)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Permission.DeletedAt == null)
                .Select(rp => new PermissionReadDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    CreatedAt = rp.Permission.CreatedAt.DateTime
                })
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Ok(permissions);
        }

        /// <summary>
        /// Récupère tous les rôles ayant une permission spécifique
        /// </summary>
        /// <param name="permissionId">ID de la permission</param>
        /// <returns>Liste des rôles ayant cette permission</returns>
        [HttpGet("permission/{permissionId}")]
        public async Task<ActionResult<IEnumerable<RoleReadDto>>> GetPermissionRoles(int permissionId)
        {
            // Vérifier que la permission existe
            var permissionExists = await _db.Permissions
                .AnyAsync(p => p.Id == permissionId && p.DeletedAt == null);
            
            if (!permissionExists)
                return NotFound(new { Message = "Permission non trouvée" });

            // Récupérer les rôles ayant cette permission
            var roles = await _db.RolesPermissions
                .AsNoTracking()
                .Where(rp => rp.PermissionId == permissionId && rp.DeletedAt == null)
                .Include(rp => rp.Role)
                .Where(rp => rp.Role.DeletedAt == null)
                .Select(rp => new RoleReadDto
                {
                    Id = rp.Role.Id,
                    Name = rp.Role.Name,
                    Description = rp.Role.Description,
                    CreatedAt = rp.Role.CreatedAt.DateTime
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return Ok(roles);
        }

        /// <summary>
        /// Assigne une permission à un rôle
        /// </summary>
        /// <param name="dto">RoleId et PermissionId</param>
        /// <returns>Message de confirmation</returns>
        [HttpPost]
        public async Task<ActionResult> AssignPermissionToRole([FromBody] RolePermissionAssignDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // Vérifier que le rôle existe
            var roleExists = await _db.Roles
                .AnyAsync(r => r.Id == dto.RoleId && r.DeletedAt == null);
            
            if (!roleExists)
                return NotFound(new { Message = "Rôle non trouvé" });

            // Vérifier que la permission existe
            var permissionExists = await _db.Permissions
                .AnyAsync(p => p.Id == dto.PermissionId && p.DeletedAt == null);
            
            if (!permissionExists)
                return NotFound(new { Message = "Permission non trouvée" });

            // Vérifier si l'association existe déjà (même soft-deleted)
            var existingAssignment = await _db.RolesPermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == dto.RoleId 
                                        && rp.PermissionId == dto.PermissionId);

            if (existingAssignment != null)
            {
                if (existingAssignment.DeletedAt == null)
                {
                    return Conflict(new { Message = "Cette permission est déjà assignée au rôle" });
                }

                // Réactiver l'association soft-deleted
                existingAssignment.DeletedAt = null;
                existingAssignment.DeletedBy = null;
                existingAssignment.UpdatedAt = DateTimeOffset.UtcNow;
                existingAssignment.UpdatedBy = userId;
            }
            else
            {
                // Créer une nouvelle association
                var rolePermission = new RolesPermissions
                {
                    RoleId = dto.RoleId,
                    PermissionId = dto.PermissionId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                };

                _db.RolesPermissions.Add(rolePermission);
            }

            await _db.SaveChangesAsync();

            return Ok(new { Message = "Permission assignée avec succès au rôle" });
        }

        /// <summary>
        /// Assigne plusieurs permissions à un rôle en une seule opération
        /// </summary>
        /// <param name="dto">RoleId et liste de PermissionIds</param>
        /// <returns>Résumé de l'opération</returns>
        [HttpPost("bulk-assign")]
        public async Task<ActionResult> BulkAssignPermissions([FromBody] RolePermissionsBulkAssignDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // Vérifier que le rôle existe
            var roleExists = await _db.Roles
                .AnyAsync(r => r.Id == dto.RoleId && r.DeletedAt == null);
            
            if (!roleExists)
                return NotFound(new { Message = "Rôle non trouvé" });

            // Vérifier que toutes les permissions existent
            var validPermissions = await _db.Permissions
                .Where(p => dto.PermissionIds.Contains(p.Id) && p.DeletedAt == null)
                .Select(p => p.Id)
                .ToListAsync();

            if (validPermissions.Count != dto.PermissionIds.Count)
            {
                return BadRequest(new { Message = "Une ou plusieurs permissions n'existent pas" });
            }

            var assignedCount = 0;
            var reactivatedCount = 0;
            var skippedCount = 0;

            foreach (var permissionId in dto.PermissionIds)
            {
                var existingAssignment = await _db.RolesPermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == dto.RoleId 
                                            && rp.PermissionId == permissionId);

                if (existingAssignment != null)
                {
                    if (existingAssignment.DeletedAt != null)
                    {
                        // Réactiver
                        existingAssignment.DeletedAt = null;
                        existingAssignment.DeletedBy = null;
                        existingAssignment.UpdatedAt = DateTimeOffset.UtcNow;
                        existingAssignment.UpdatedBy = userId;
                        reactivatedCount++;
                    }
                    else
                    {
                        // Déjà assignée
                        skippedCount++;
                    }
                }
                else
                {
                    // Créer nouvelle association
                    var rolePermission = new RolesPermissions
                    {
                        RoleId = dto.RoleId,
                        PermissionId = permissionId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = userId
                    };

                    _db.RolesPermissions.Add(rolePermission);
                    assignedCount++;
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new 
            { 
                Message = "Permissions assignées avec succès",
                Assigned = assignedCount,
                Reactivated = reactivatedCount,
                Skipped = skippedCount
            });
        }

        /// <summary>
        /// Remplace toutes les permissions d'un rôle
        /// </summary>
        /// <param name="dto">RoleId et nouvelle liste de PermissionIds</param>
        /// <returns>Résumé de l'opération</returns>
        [HttpPut("replace")]
        public async Task<ActionResult> ReplaceRolePermissions([FromBody] RolePermissionsBulkAssignDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // Vérifier que le rôle existe
            var roleExists = await _db.Roles
                .AnyAsync(r => r.Id == dto.RoleId && r.DeletedAt == null);
            
            if (!roleExists)
                return NotFound(new { Message = "Rôle non trouvé" });

            // Vérifier que toutes les permissions existent
            var validPermissions = await _db.Permissions
                .Where(p => dto.PermissionIds.Contains(p.Id) && p.DeletedAt == null)
                .Select(p => p.Id)
                .ToListAsync();

            if (validPermissions.Count != dto.PermissionIds.Count)
            {
                return BadRequest(new { Message = "Une ou plusieurs permissions n'existent pas" });
            }

            // Supprimer toutes les permissions actuelles
            var currentPermissions = await _db.RolesPermissions
                .Where(rp => rp.RoleId == dto.RoleId && rp.DeletedAt == null)
                .ToListAsync();

            foreach (var rp in currentPermissions)
            {
                rp.DeletedAt = DateTimeOffset.UtcNow;
                rp.DeletedBy = userId;
            }

            // Assigner les nouvelles permissions
            var assignedCount = 0;
            var reactivatedCount = 0;

            foreach (var permissionId in dto.PermissionIds)
            {
                var existingAssignment = await _db.RolesPermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == dto.RoleId 
                                            && rp.PermissionId == permissionId);

                if (existingAssignment != null)
                {
                    // Réactiver (vient d'être supprimé ou était déjà supprimé)
                    existingAssignment.DeletedAt = null;
                    existingAssignment.DeletedBy = null;
                    existingAssignment.UpdatedAt = DateTimeOffset.UtcNow;
                    existingAssignment.UpdatedBy = userId;
                    reactivatedCount++;
                }
                else
                {
                    // Créer nouvelle association
                    var rolePermission = new RolesPermissions
                    {
                        RoleId = dto.RoleId,
                        PermissionId = permissionId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = userId
                    };

                    _db.RolesPermissions.Add(rolePermission);
                    assignedCount++;
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new 
            { 
                Message = "Permissions remplacées avec succès",
                Removed = currentPermissions.Count,
                Assigned = assignedCount,
                Reactivated = reactivatedCount
            });
        }

        /// <summary>
        /// Retire une permission d'un rôle (soft delete)
        /// </summary>
        /// <param name="dto">RoleId et PermissionId</param>
        /// <returns>204 No Content</returns>
        [HttpDelete]
        public async Task<IActionResult> RemovePermissionFromRole([FromBody] RolePermissionAssignDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            var rolePermission = await _db.RolesPermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == dto.RoleId 
                                        && rp.PermissionId == dto.PermissionId 
                                        && rp.DeletedAt == null);

            if (rolePermission == null)
                return NotFound(new { Message = "Cette association rôle-permission n'existe pas" });

            // Soft delete
            rolePermission.DeletedAt = DateTimeOffset.UtcNow;
            rolePermission.DeletedBy = userId;

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
