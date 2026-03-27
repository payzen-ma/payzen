using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payzen.Domain.Entities.Auth;
using Payzen.Application.DTOs.Auth;
using Payzen.Application.Interfaces;
using Payzen.Api.Extensions;
using Payzen.Api.Authorization;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Api.Controllers.Auth;

// ── Users ─────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly AppDbContext _db;

    public UsersController(IAuthService auth, AppDbContext db)
    {
        _auth = auth;
        _db = db;
    }

    // Récupère tous les utilisateurs (sans les rôles et permissions détaillés)
    [HttpGet]
    [HasPermission("READ_USERS")]
    public async Task<ActionResult> GetAll()
    {
        var r = await _auth.GetAllUsersAsync();
        
        return r.Success ? Ok(r.Data) : BadRequest(new { r.Error });
    }

    // Récupère un utilisateur par son ID (sans les rôles et permissions détaillés)
    [HttpGet("{id:int}")]
    [HasPermission("VIEW_USERS")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _auth.GetUserByIdAsync(id);
        
        return r.Success ? Ok(r.Data) : NotFound(new { r.Error });
    }

    // Crée un nouvel utilisateur
    [HttpPost]
    [HasPermission("CREATE_USERS")]
    public async Task<ActionResult> Create([FromBody] UserCreateDto dto)
    {
        var r = await _auth.CreateUserAsync(dto, User.GetUserId());
        
        return r.Success ? Ok(r.Data) : BadRequest(new { r.Error });
    }

    // Invite (ou création) d'un compte utilisateur pour un employé existant
    // Front attend : POST /api/users/invite { email, role, companyId }
    [HttpPost("invite")]
    [HasPermission("CREATE_USERS")]
    public async Task<ActionResult> Invite([FromBody] UserInviteDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null, ct);
        
        if (company == null)
            return NotFound(new { Message = "Company introuvable." });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.CompanyId == dto.CompanyId && e.Email == dto.Email && e.DeletedAt == null, ct);
        
        if (employee == null)
            return BadRequest(new { Message = "Aucun employé trouvé pour cet email et cette société." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.DeletedAt == null, ct);

        var createdBy = User.GetUserId();
        
        if (user == null)
        {
            var baseUsername = dto.Email.Split('@')[0];
            
            if (baseUsername.Length < 3) 
                baseUsername = baseUsername.PadRight(3, 'u');

            var usernameCandidate = baseUsername;
            
            var suffix = 1;
            
            while (await _db.Users.AnyAsync(u => u.Username == usernameCandidate && u.DeletedAt == null, ct))
            {
                usernameCandidate = $"{baseUsername}{suffix}";
                suffix++;
            }

            user = new Users
            {
                Username = usernameCandidate,
                Email = dto.Email,
                PasswordHash = company.AuthType == "C" ? null : BCrypt.Net.BCrypt.HashPassword(System.Guid.NewGuid().ToString("N")),
                IsActive = true,
                EmployeeId = employee.Id,
                CreatedBy = createdBy,
                Source = company.AuthType == "C" ? "entra" : null
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            user.EmployeeId = employee.Id;
            user.IsActive = true;
            if (company.AuthType == "C")
            {
                user.PasswordHash = null;
                user.Source = "entra";
            }
            await _db.SaveChangesAsync(ct);
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == dto.RoleId && r.DeletedAt == null, ct);
        if (role == null)
            return BadRequest(new { Message = "Role introuvable." });

        var relation = await _db.UsersRoles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, ct);

        if (relation == null)
        {
            _db.UsersRoles.Add(new UsersRoles
            {
                UserId = user.Id,
                RoleId = role.Id,
                CreatedBy = createdBy
            });
        }
        else
        {
            relation.DeletedAt = null;
            relation.DeletedBy = null;
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPatch("{id:int}")]
    [HasPermission("EDIT_USERS")]
    public async Task<ActionResult> Patch(int id, [FromBody] UserUpdateDto dto)
    {
        var r = await _auth.UpdateUserAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { r.Error });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("DELETE_USERS")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _auth.DeleteUserAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }
}

// ── Roles ─────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IAuthService _auth;
    public RolesController(IAuthService auth) => _auth = auth;

    [HttpGet]
    [HasPermission("READ_ROLES")]
    public async Task<ActionResult> GetAll()
    {
        var r = await _auth.GetAllRolesAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { r.Error });
    }

    [HttpGet("{id:int}")]
    [HasPermission("VIEW_ROLES")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _auth.GetRoleSummaryAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { r.Error });
    }

    [HttpPost]
    [HasPermission("CREATE_ROLES")]
    public async Task<ActionResult> Create([FromBody] RoleCreateDto dto)
    {
        var r = await _auth.CreateRoleAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { r.Error });
    }

    [HttpPut("{id:int}")]
    [HasPermission("EDIT_ROLES")]
    public async Task<ActionResult> Patch(int id, [FromBody] RoleUpdateDto dto)
    {
        var r = await _auth.UpdateRoleAsync(id, dto);
        return r.Success ? Ok(r.Data) : BadRequest(new { r.Error });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("DELETE_ROLES")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _auth.DeleteRoleAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }

    [HttpGet("{id:int}/users")]
    [HasPermission("VIEW_ROLES_USERS")]
    public async Task<ActionResult> GetUsersInRole(int id)
    {
        var r = await _auth.GetUsersInRoleAsync(id);
        return r.Success ? Ok(r.Data) : BadRequest(new { r.Error });
    }
}

// ── Permissions ───────────────────────────────────────────────────────────────

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IAuthService _auth;
    public PermissionsController(IAuthService auth) => _auth = auth;

    [HttpGet]
    [HasPermission("READ_PERMISSIONS")]
    public async Task<ActionResult> GetAll()
    {
        var r = await _auth.GetAllPermissionsAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { r.Error });
    }

    [HttpPost]
    [HasPermission("CREATE_PERMISSIONS")]
    public async Task<ActionResult> Create([FromBody] PermissionCreateDto dto)
    {
        var r = await _auth.CreatePermissionAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { r.Error });
    }

    [HttpPut("{id:int}")]
    [HasPermission("EDIT_PERMISSIONS")]
    public async Task<ActionResult> Update(int id, [FromBody] PermissionUpdateDto dto)
    {
        var r = await _auth.UpdatePermissionAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { r.Error });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("DELETE_PERMISSIONS")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _auth.DeletePermissionAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }
}

// ── Roles ↔ Permissions ───────────────────────────────────────────────────────

[ApiController]
[Route("api/roles-permissions")]
[Authorize]
public class RolesPermissionsController : ControllerBase
{
    private readonly IAuthService _auth;
    public RolesPermissionsController(IAuthService auth) => _auth = auth;

    [HttpGet("role/{roleId:int}")]
    [HasPermission("READ_ROLES_PERMISSIONS")]
    public async Task<ActionResult> GetPermissionsForRole(int roleId)
    {
        var r = await _auth.GetPermissionsForRoleAsync(roleId);
        return r.Success ? Ok(r.Data) : BadRequest(new { r.Error });
    }

    [HttpPost("assign")]
    [HasPermission("ASSIGN_ROLES_PERMISSIONS")]
    public async Task<ActionResult> Assign([FromBody] RolePermissionAssignDto dto)
    {
        var r = await _auth.AssignPermissionToRoleAsync(dto, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }

    [HttpPost("bulk-assign")]
    [HasPermission("BULK_ASSIGN_ROLES_PERMISSION")]
    public async Task<ActionResult> BulkAssign([FromBody] RolePermissionsBulkAssignDto dto)
    {
        var r = await _auth.BulkAssignPermissionsToRoleAsync(dto, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }

    [HttpDelete("{roleId:int}/{permissionId:int}")]
    [HasPermission("DELETE_ROLES_PERMISSIONS")]
    public async Task<ActionResult> Revoke(int roleId, int permissionId)
    {
        var r = await _auth.RevokePermissionFromRoleAsync(roleId, permissionId);
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }
}

// ── Users ↔ Roles ─────────────────────────────────────────────────────────────

[ApiController]
[Route("api/users-roles")]
[Authorize]
public class UsersRolesController : ControllerBase
{
    private readonly IAuthService _auth;
    public UsersRolesController(IAuthService auth) => _auth = auth;

    [HttpGet("employee/{employeeId:int}")]
    [HasPermission("READ_USER_ROLES")]
    public async Task<ActionResult> GetEmployeeRoles(int employeeId)
    {
        var r = await _auth.GetRolesForEmployeeAsync(employeeId);
        if (!r.Success) return NotFound(new { Message = r.Error });
        return Ok(r.Data);
    }

    [HttpPost("employee/{employeeId:int}/assign")]
    [HasPermission("ASSIGN_USERS_ROLES")]
    public async Task<ActionResult> AssignRolesToEmployee(int employeeId, [FromBody] int[] roleIds)
    {
        var r = await _auth.AssignRolesToEmployeeAsync(employeeId, roleIds ?? Array.Empty<int>(), User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }

    [HttpDelete("employee/{employeeId:int}/{roleId:int}")]
    [HasPermission("DELETE_USER_ROLES")]
    public async Task<ActionResult> RevokeFromEmployee(int employeeId, int roleId)
    {
        var r = await _auth.RevokeRoleFromEmployeeAsync(employeeId, roleId);
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }

    [HttpPost("assign")]
    [HasPermission("ASSIGN_USERS_ROLES")]
    public async Task<ActionResult> Assign([FromBody] UserRoleAssignDto dto)
    {
        var r = await _auth.AssignRoleToUserAsync(dto, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }

    [HttpPost("bulk-assign")]
    [HasPermission("BULK_ASSIGN_USERS_ROLES")]
    public async Task<ActionResult> BulkAssign([FromBody] UserRolesBulkAssignDto dto)
    {
        var r = await _auth.BulkAssignRolesToUserAsync(dto, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }

    [HttpPut("replace")]
    [HasPermission("UPDATE_USERS_ROLES")]
    public async Task<ActionResult> Replace([FromBody] UserRoleRepalceDto dto)
    {
        var r = await _auth.RepalceRolesForUserAsync(dto, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }

    [HttpDelete("{userId:int}/{roleId:int}")]
    [HasPermission("DELETE_USER_ROLES")]
    public async Task<ActionResult> Revoke(int userId, int roleId)
    {
        var r = await _auth.RevokeRoleFromUserAsync(userId, roleId);
        return r.Success ? Ok() : BadRequest(new { r.Error });
    }
}
