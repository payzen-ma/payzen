using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Permissions;
using payzen_backend.Models.Permissions.Dtos;
using payzen_backend.Extensions;
using payzen_backend.Models.Users.Dtos;
using payzen_backend.Services;
using System.Linq;
using System.Threading.Tasks;

namespace payzen_backend.Controllers.Auth
{
    [Route("api/users-roles")]
    [ApiController]
    [Authorize]
    public class UsersRolesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmployeeEventLogService _employeeEventLogService;

        public UsersRolesController(AppDbContext db, EmployeeEventLogService employeeEventLogService)
        {
            _db = db;
            _employeeEventLogService = employeeEventLogService;
        }

        /// <summary>
        /// Récupère tous les rôles assignés à un utilisateur
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Liste des rôles de l'utilisateur</returns>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<RoleReadDto>>> GetUserRoles(int userId)
        {
            // Vérifier que l'utilisateur existe et est actif
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
            Console.WriteLine($"Try to get user with id : {userId}");
            if (user == null)
                return NotFound(new { Message = "Utilisateur non trouvé" });

            Console.WriteLine($"User trouvé ! {user.Username}");
            // Récupérer les rôles de l'utilisateur
            var roles = await _db.UsersRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId && ur.DeletedAt == null)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.DeletedAt == null)
                .Select(ur => new RoleReadDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Description = ur.Role.Description,
                    CreatedAt = ur.Role.CreatedAt.DateTime
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return Ok(roles);
        }

        /// <summary>
        /// Récupère tous les utilisateurs ayant un rôle spécifique
        /// </summary>
        /// <param name="roleId">ID du rôle</param>
        /// <returns>Liste des utilisateurs ayant ce rôle</returns>
        [HttpGet("role/{roleId}")]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> GetRoleUsers(int roleId)
        {
            // Vérifier que le rôle existe
            var roleExists = await _db.Roles
                .AnyAsync(r => r.Id == roleId && r.DeletedAt == null);

            if (!roleExists)
                return NotFound(new { Message = "Rôle non trouvé" });

            // Récupérer les utilisateurs ayant ce rôle
            var users = await _db.UsersRoles
                .AsNoTracking()
                .Where(ur => ur.RoleId == roleId && ur.DeletedAt == null)
                .Include(ur => ur.User)
                .Where(ur => ur.User.DeletedAt == null)
                .Select(ur => new UserReadDto
                {
                    Id = ur.User.Id,
                    Username = ur.User.Username,
                    Email = ur.User.Email,
                    IsActive = ur.User.IsActive,
                    CreatedAt = ur.User.CreatedAt.DateTime
                })
                .OrderBy(u => u.Username)
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Assigne un rôle à un utilisateur
        /// </summary>
        /// <param name="dto">UserId et RoleId</param>
        /// <returns>Message de confirmation</returns>
        [HttpPost]
        public async Task<ActionResult> AssignRoleToUser([FromBody] UserRoleAssignDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.GetUserId();

            // Vérifier que l'utilisateur existe et est actif
            var user = await _db.Users
                .Where(u => u.Id == dto.UserId && u.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { Message = "Utilisateur non trouvé" });

            if (!user.IsActive)
                return BadRequest(new { Message = "L'utilisateur est désactivé" });

            // Vérifier que le rôle existe (récupérer le role pour le logging)
            var role = await _db.Roles
                .Where(r => r.Id == dto.RoleId && r.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (role == null)
                return NotFound(new { Message = "Rôle non trouvé" });

            // Vérifier si l'association existe déjà (même soft-deleted)
            var existingAssignment = await _db.UsersRoles
                .FirstOrDefaultAsync(ur => ur.UserId == dto.UserId && ur.RoleId == dto.RoleId);

            if (existingAssignment != null)
            {
                if (existingAssignment.DeletedAt == null)
                {
                    return Conflict(new { Message = "L'utilisateur possède déjà ce rôle" });
                }

                // Réactiver l'association soft-deleted
                existingAssignment.DeletedAt = null;
                existingAssignment.DeletedBy = null;
                existingAssignment.UpdatedAt = DateTimeOffset.UtcNow;
                existingAssignment.UpdatedBy = currentUserId;

                await _db.SaveChangesAsync();

                // Log event for employee if exists
                var empId = await _db.Users.Where(u => u.Id == dto.UserId).Select(u => u.EmployeeId).FirstOrDefaultAsync();
                if (empId.HasValue)
                {
                    await _employeeEventLogService.LogEventAsync(
                        empId.Value,
                        "Role_Assigned",
                        null,
                        null,
                        role.Name,
                        role.Id,
                        currentUserId
                    );
                }

                return Ok(new { Message = "Rôle réactivé pour l'utilisateur" });
            }
            else
            {
                // Créer une nouvelle association
                var userRole = new UsersRoles
                {
                    UserId = dto.UserId,
                    RoleId = dto.RoleId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = currentUserId
                };

                _db.UsersRoles.Add(userRole);
                await _db.SaveChangesAsync();

                // Log event for employee if exists
                var empId = await _db.Users.Where(u => u.Id == dto.UserId).Select(u => u.EmployeeId).FirstOrDefaultAsync();
                if (empId.HasValue)
                {
                    await _employeeEventLogService.LogEventAsync(
                        empId.Value,
                        "Role_Assigned",
                        null,
                        null,
                        role.Name,
                        role.Id,
                        currentUserId
                    );
                }

                return Ok(new { Message = "Rôle assigné avec succès" });
            }
        }

        /// <summary>
        /// Assigne plusieurs rôles à un utilisateur en une seule opération
        /// </summary>
        /// <param name="dto">UserId et liste de RoleIds</param>
        /// <returns>Résumé de l'opération</returns>
        [HttpPost("bulk-assign")]
        public async Task<ActionResult> BulkAssignRoles([FromBody] UserRolesBulkAssignDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.GetUserId();

            // Vérifier que l'utilisateur existe et est actif
            var user = await _db.Users
                .Where(u => u.Id == dto.UserId && u.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { Message = "Utilisateur non trouvé" });

            if (!user.IsActive)
                return BadRequest(new { Message = "L'utilisateur est désactivé" });

            // Vérifier que tous les rôles existent et construire map id->name
            var rolesMap = await _db.Roles
                .Where(r => dto.RoleIds.Contains(r.Id) && r.DeletedAt == null)
                .ToDictionaryAsync(r => r.Id, r => r.Name);

            if (rolesMap.Count != dto.RoleIds.Count)
            {
                return BadRequest(new { Message = "Un ou plusieurs rôles n'existent pas" });
            }

            var assignedCount = 0;
            var reactivatedCount = 0;
            var skippedCount = 0;

            var empId = await _db.Users.Where(u => u.Id == dto.UserId).Select(u => u.EmployeeId).FirstOrDefaultAsync();

            foreach (var roleId in dto.RoleIds)
            {
                var existingAssignment = await _db.UsersRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == dto.UserId && ur.RoleId == roleId);

                if (existingAssignment != null)
                {
                    if (existingAssignment.DeletedAt != null)
                    {
                        // Réactiver
                        existingAssignment.DeletedAt = null;
                        existingAssignment.DeletedBy = null;
                        existingAssignment.UpdatedAt = DateTimeOffset.UtcNow;
                        existingAssignment.UpdatedBy = currentUserId;
                        reactivatedCount++;

                        if (empId.HasValue)
                        {
                            await _employeeEventLogService.LogEventAsync(
                                empId.Value,
                                "Role_Assigned",
                                null,
                                null,
                                rolesMap[roleId],
                                roleId,
                                currentUserId
                            );
                        }
                    }
                    else
                    {
                        // Déjà assigné
                        skippedCount++;
                    }
                }
                else
                {
                    // Créer nouvelle association
                    var userRole = new UsersRoles
                    {
                        UserId = dto.UserId,
                        RoleId = roleId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = currentUserId
                    };

                    _db.UsersRoles.Add(userRole);
                    assignedCount++;

                    if (empId.HasValue)
                    {
                        await _employeeEventLogService.LogEventAsync(
                            empId.Value,
                            "Role_Assigned",
                            null,
                            null,
                            rolesMap[roleId],
                            roleId,
                            currentUserId
                        );
                    }
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                Message = "Rôles assignés avec succès",
                Assigned = assignedCount,
                Reactivated = reactivatedCount,
                Skipped = skippedCount
            });
        }

        /// <summary>
        /// Remplace tous les rôles d'un utilisateur
        /// </summary>
        /// <param name="dto">UserId et nouvelle liste de RoleIds</param>
        /// <returns>Résumé de l'opération</returns>
        [HttpPut("replace")]
        public async Task<ActionResult> ReplaceUserRoles([FromBody] UserRolesBulkAssignDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.GetUserId();

            // Vérifier que l'utilisateur existe et est actif
            var user = await _db.Users
                .Where(u => u.Id == dto.UserId && u.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { Message = "Utilisateur non trouvé" });

            if (!user.IsActive)
                return BadRequest(new { Message = "L'utilisateur est désactivé" });

            // Vérifier que tous les rôles existent et construire map id->name
            var rolesMap = await _db.Roles
                .Where(r => dto.RoleIds.Contains(r.Id) && r.DeletedAt == null)
                .ToDictionaryAsync(r => r.Id, r => r.Name);

            if (rolesMap.Count != dto.RoleIds.Count)
            {
                return BadRequest(new { Message = "Un ou plusieurs rôles n'existent pas" });
            }

            // Supprimer tous les rôles actuels (charger avec Role pour logging)
            var currentRoles = await _db.UsersRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == dto.UserId && ur.DeletedAt == null)
                .ToListAsync();

            var empId = await _db.Users.Where(u => u.Id == dto.UserId).Select(u => u.EmployeeId).FirstOrDefaultAsync();

            foreach (var ur in currentRoles)
            {
                ur.DeletedAt = DateTimeOffset.UtcNow;
                ur.DeletedBy = currentUserId;

                // Log removal if employee exists and role present
                if (empId.HasValue && ur.Role != null)
                {
                    await _employeeEventLogService.LogEventAsync(
                        empId.Value,
                        "Role_Removed",
                        ur.Role.Name,
                        ur.Role.Id,
                        null,
                        null,
                        currentUserId
                    );
                }
            }

            // Assigner les nouveaux rôles
            var assignedCount = 0;
            var reactivatedCount = 0;

            foreach (var roleId in dto.RoleIds)
            {
                var existingAssignment = await _db.UsersRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == dto.UserId && ur.RoleId == roleId);

                if (existingAssignment != null)
                {
                    // Réactiver
                    existingAssignment.DeletedAt = null;
                    existingAssignment.DeletedBy = null;
                    existingAssignment.UpdatedAt = DateTimeOffset.UtcNow;
                    existingAssignment.UpdatedBy = currentUserId;
                    reactivatedCount++;

                    if (empId.HasValue)
                    {
                        await _employeeEventLogService.LogEventAsync(
                            empId.Value,
                            "Role_Assigned",
                            null,
                            null,
                            rolesMap[roleId],
                            roleId,
                            currentUserId
                        );
                    }
                }
                else
                {
                    // Créer nouvelle association
                    var userRole = new UsersRoles
                    {
                        UserId = dto.UserId,
                        RoleId = roleId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = currentUserId
                    };

                    _db.UsersRoles.Add(userRole);
                    assignedCount++;

                    if (empId.HasValue)
                    {
                        await _employeeEventLogService.LogEventAsync(
                            empId.Value,
                            "Role_Assigned",
                            null,
                            null,
                            rolesMap[roleId],
                            roleId,
                            currentUserId
                        );
                    }
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                Message = "Rôles remplacés avec succès",
                Removed = currentRoles.Count,
                Assigned = assignedCount,
                Reactivated = reactivatedCount
            });
        }

        /// <summary>
        /// Retire un rôle d'un utilisateur (soft delete)
        /// </summary>
        /// <param name="dto">UserId et RoleId</param>
        /// <returns>204 No Content</returns>
        [HttpDelete]
        public async Task<IActionResult> RemoveRoleFromUser([FromBody] UserRoleAssignDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.GetUserId();

            var userRole = await _db.UsersRoles
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == dto.UserId
                                        && ur.RoleId == dto.RoleId
                                        && ur.DeletedAt == null);

            if (userRole == null)
                return NotFound(new { Message = "Cette association utilisateur-rôle n'existe pas" });

            // Soft delete
            userRole.DeletedAt = DateTimeOffset.UtcNow;
            userRole.DeletedBy = currentUserId;

            await _db.SaveChangesAsync();

            // Log event for employee if exists
            var empId = await _db.Users.Where(u => u.Id == dto.UserId).Select(u => u.EmployeeId).FirstOrDefaultAsync();
            if (empId.HasValue && userRole.Role != null)
            {
                await _employee_eventLog_safe_call(empId.Value, "Role_Removed", userRole.Role.Name, userRole.Role.Id, currentUserId);
            }

            return NoContent();
        }

        /// <summary>
        /// Récupère tous les rôles d'un employé (cherche par EmployeeId)
        /// </summary>
        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetEmployeeRoles(int employeeId)
        {
            // Trouver le user associé à cet employé
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.EmployeeId == employeeId && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Utilisateur non trouvé pour cet employé" });

            // Récupérer les rôles
            var roles = await _db.UsersRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == user.Id && ur.DeletedAt == null)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.DeletedAt == null)
                .Select(ur => new
                {
                    UserId = ur.UserId,
                    RoleId = ur.RoleId,
                    Role = new
                    {
                        Id = ur.Role.Id,
                        Name = ur.Role.Name,
                        Description = ur.Role.Description,
                        CreatedAt = ur.Role.CreatedAt.DateTime
                    }
                })
                .OrderBy(r => r.Role.Name)
                .ToListAsync();

            return Ok(roles);
        }

        /// <summary>
        /// Assigne des rôles à un employé (cherche par EmployeeId)
        /// </summary>
        [HttpPost("employee/{employeeId}/assign")]
        public async Task<ActionResult> AssignRolesToEmployee(int employeeId, [FromBody] int[] roleIds)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.EmployeeId == employeeId && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Utilisateur non trouvé pour cet employé" });

            var dto = new UserRolesBulkAssignDto { UserId = user.Id, RoleIds = roleIds.ToList() };
            return await BulkAssignRoles(dto);
        }

        [HttpDelete]
        /// <param name="empId"></param>
        /// <param name="eventName"></param>
        /// <param name="oldValue"></param>
        /// <param name="oldValueId"></param>
        /// <param name="createdBy"></param>
        /// <returns></returns>
        // helper to safely call logging without breaking main flow
        private async Task _employee_eventLog_safe_call(int empId, string eventName, string? oldValue, int? oldValueId, int createdBy)
        {
            try
            {
                await _employeeEventLogService.LogEventAsync(empId, eventName, oldValue, oldValueId, null, null, createdBy);
            }
            catch
            {
                // don't block main operation if logging fails
            }
        }
    }
}