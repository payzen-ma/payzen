using Payzen.Application.Common;
using Payzen.Application.DTOs.Auth;
using Payzen.Domain.Entities.Auth;

namespace Payzen.Application.Interfaces;

/// <summary>
/// Authentification, gestion des utilisateurs, rôles et permissions.
/// </summary>
public interface IAuthService
{
    // ── Login ────────────────────────────────────────────────
    Task<ServiceResult<LoginResponse>> LoginWithEntraAsync(EntraLoginRequestDto dto, CancellationToken ct = default);
    Task<ServiceResult<UserReadDto>> GetMeAsync(int userId, CancellationToken ct = default);

    // ── Users ────────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<UserReadDto>>> GetAllUsersAsync(CancellationToken ct = default);
    Task<ServiceResult<UserReadDto>> GetUserByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<UserReadDto>> CreateUserAsync(UserCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<UserReadDto>> UpdateUserAsync(
        int id,
        UserUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeleteUserAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── Roles ────────────────────────────────────────────────
    Task<ServiceResult<IEnumerable<RoleReadDto>>> GetAllRolesAsync(CancellationToken ct = default);
    Task<ServiceResult<RoleSummaryDto>> GetRoleSummaryAsync(int roleId, CancellationToken ct = default);
    Task<ServiceResult<RoleReadDto>> CreateRoleAsync(RoleCreateDto dto, int createdBy, CancellationToken ct = default);
    Task<ServiceResult<RoleReadDto>> UpdateRoleAsync(int id, RoleUpdateDto dto, CancellationToken ct = default);
    Task<ServiceResult> DeleteRoleAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── Permissions ──────────────────────────────────────────
    Task<ServiceResult<IEnumerable<PermissionReadDto>>> GetAllPermissionsAsync(CancellationToken ct = default);
    Task<ServiceResult<PermissionReadDto>> CreatePermissionAsync(
        PermissionCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult<PermissionReadDto>> UpdatePermissionAsync(
        int id,
        PermissionUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> DeletePermissionAsync(int id, int deletedBy, CancellationToken ct = default);

    // ── Role ↔ Permission ────────────────────────────────────
    Task<ServiceResult<IEnumerable<RolePermissionSimpleDto>>> GetPermissionsForRoleAsync(
        int roleId,
        CancellationToken ct = default
    );
    Task<ServiceResult<RolePermissionReadDto>> AssignPermissionToRoleAsync(
        RolePermissionAssignDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> BulkAssignPermissionsToRoleAsync(
        RolePermissionsBulkAssignDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> RevokePermissionFromRoleAsync(int roleId, int permissionId, CancellationToken ct = default);

    // ── User ↔ Role ──────────────────────────────────────────
    Task<ServiceResult<RoleUsersDto>> GetUsersInRoleAsync(int roleId, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<UserRoleSimpleDto>>> GetRolesForUserAsync(
        int userId,
        CancellationToken ct = default
    );
    Task<ServiceResult<UserRoleReadDto>> AssignRoleToUserAsync(
        UserRoleAssignDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> BulkAssignRolesToUserAsync(
        UserRolesBulkAssignDto dto,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> RevokeRoleFromUserAsync(int userId, int roleId, CancellationToken ct = default);
    Task<ServiceResult> RepalceRolesForUserAsync(UserRoleRepalceDto dto, int updatedBy, CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<UserRoleSimpleDto>>> GetRolesForEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    );
    Task<ServiceResult> AssignRolesToEmployeeAsync(
        int employeeId,
        IEnumerable<int> roleIds,
        int createdBy,
        CancellationToken ct = default
    );
    Task<ServiceResult> RevokeRoleFromEmployeeAsync(int employeeId, int roleId, CancellationToken ct = default);
}

public interface IUserInviteService
{
    Task<ServiceResult> InviteUserAsync(UserInviteDto dto, int createdBy, CancellationToken ct = default);
}
