using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Payzen.Application.DTOs.Auth;

// ════════════════════════════════════════════════════════════
// LOGIN (Entra)
// ════════════════════════════════════════════════════════════

/// <summary>
/// Echange d'identité : Frontend (MSAL) envoie les infos Entra, backend renvoie un JWT Payzen.
/// </summary>
public class EntraLoginRequestDto
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "ExternalId est requis")]
    public required string ExternalId { get; set; }
}

public class LoginResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = null!;
}

public class UserInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public bool isCabinetExpert { get; set; }
    public int? EmployeeId { get; set; }
    public int? EmployeeCategoryId { get; set; }
    public string? Mode { get; set; }
    public int companyId { get; set; }
}

/// <summary>
/// Réponse de /api/auth/me (parité avec l'ancien backend).
/// </summary>
public class MeResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

// ════════════════════════════════════════════════════════════
// USERS
// ════════════════════════════════════════════════════════════

public class UserCreateDto
{
    [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Le nom d'utilisateur doit contenir entre 3 et 50 caractères")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
    public required string Email { get; set; }

    public required bool IsActive { get; set; }
}

public class UserUpdateDto
{
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Le nom d'utilisateur doit contenir entre 3 et 50 caractères")]
    public string? Username { get; set; }

    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
    public string? Email { get; set; }

    public bool? IsActive { get; set; }
}

public class UserInviteDto
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Le companyId est requis")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Le rôle est requis")]
    public int RoleId { get; set; }
}

public class UserReadDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// ROLES
// ════════════════════════════════════════════════════════════

public class RoleCreateDto
{
    [Required(ErrorMessage = "Le nom du rôle est requis")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Le nom du rôle doit contenir entre 2 et 50 caractères")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "La description est requise")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "La description doit contenir entre 10 et 500 caractères")]
    public required string Description { get; set; }
}

public class RoleUpdateDto
{
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Le nom du rôle doit contenir entre 2 et 50 caractères")]
    public string? Name { get; set; }

    [StringLength(500, MinimumLength = 10, ErrorMessage = "La description doit contenir entre 10 et 500 caractères")]
    public string? Description { get; set; }
}

public class RoleReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Vue résumée d'un rôle pour les listes.
/// Note : [JsonPropertyName] conserve la casse attendue par le front ("UsersLength").
/// </summary>
public class RoleSummaryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("userCount")]
    public int UserCount { get; set; }

    // Alias pour compatibilité front existant
    [JsonPropertyName("UsersLength")]
    public int UsersLength { get; set; }
}

// ════════════════════════════════════════════════════════════
// PERMISSIONS
// ════════════════════════════════════════════════════════════

public class PermissionCreateDto
{
    [Required(ErrorMessage = "Le nom de la permission est requis")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Le nom doit contenir entre 3 et 100 caractères")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "La description est requise")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "La description doit contenir entre 10 et 500 caractères")]
    public required string Description { get; set; }

    public string? Action { get; set; }
    public string? Resource { get; set; }
}

public class PermissionUpdateDto
{
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Le nom doit contenir entre 3 et 100 caractères")]
    public string? Name { get; set; }

    [StringLength(500, MinimumLength = 10, ErrorMessage = "La description doit contenir entre 10 et 500 caractères")]
    public string? Description { get; set; }
}

public class PermissionReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Resource { get; set; }
    public string? Action { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// ROLE ↔ PERMISSION
// ════════════════════════════════════════════════════════════

public class RolePermissionCreateDto
{
    [Required(ErrorMessage = "L'ID du rôle est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du rôle doit être supérieur à 0")]
    public int RoleId { get; set; }

    [Required(ErrorMessage = "L'ID de la permission est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de la permission doit être supérieur à 0")]
    public int PermissionId { get; set; }
}

/// <summary>Assignation d'une permission à un rôle (via la route)</summary>
public class RolePermissionAssignDto
{
    [Required(ErrorMessage = "L'ID du rôle est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du rôle doit être valide")]
    public int RoleId { get; set; }

    [Required(ErrorMessage = "L'ID de la permission est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de la permission doit être valide")]
    public int PermissionId { get; set; }
}

/// <summary>Assignation en masse de permissions à un rôle</summary>
public class RolePermissionsBulkAssignDto
{
    [Required(ErrorMessage = "L'ID du rôle est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du rôle doit être valide")]
    public int RoleId { get; set; }

    [Required(ErrorMessage = "Au moins une permission doit être spécifiée")]
    [MinLength(1, ErrorMessage = "Au moins une permission doit être spécifiée")]
    // Nullable pour tolérer les payloads front contenant des `null` dans la liste.
    // Le service filtrera les valeurs null / <= 0.
    public List<int?> PermissionIds { get; set; } = new();
}

public class RolePermissionReadDto
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string PermissionDescription { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Liste allégée des permissions d'un rôle — GET /api/roles/{id}/permissions</summary>
public class RolePermissionSimpleDto
{
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string PermissionDescription { get; set; } = string.Empty;
}

public class BulkAssignResultDto
{
    public int Assigned { get; set; }
    public int Reactivated { get; set; }
    public int Skipped { get; set; }
}

// ════════════════════════════════════════════════════════════
// USER ↔ ROLE
// ════════════════════════════════════════════════════════════

public class UserRoleCreateDto
{
    [Required(ErrorMessage = "L'ID de l'utilisateur est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'utilisateur doit être supérieur à 0")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "L'ID du rôle est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du rôle doit être supérieur à 0")]
    public int RoleId { get; set; }
}

/// <summary>Assignation d'un rôle à un utilisateur (via la route)</summary>
public class UserRoleAssignDto
{
    [Required(ErrorMessage = "L'ID de l'utilisateur est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'utilisateur doit être valide")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "L'ID du rôle est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID du rôle doit être valide")]
    public int RoleId { get; set; }
}

/// <summary>Assignation en masse de rôles à un utilisateur</summary>
public class UserRolesBulkAssignDto
{
    [Required(ErrorMessage = "L'ID de l'utilisateur est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'utilisateur doit être valide")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Au moins un rôle doit être spécifié")]
    [MinLength(1, ErrorMessage = "Au moins un rôle doit être spécifié")]
    public List<int> RoleIds { get; set; } = new();
}

/// <summary>Remplace tous les rôles d'un utilisateur par un seul rôle spécifié</summary>
public class UserRoleRepalceDto
{
    public int UserId { get; set; }
    public int RoleID { get; set; }
}

public class UserRoleReadDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string RoleDescription { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Rôles d'un utilisateur — GET /api/users/{id}/roles</summary>
public class UserRoleSimpleDto
{
    /// <summary>Utilisateur auquel les rôles sont attachés (toujours renseigné pour GET par userId).</summary>
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleDescription { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

/// <summary>Utilisateurs d'un rôle — GET /api/roles/{id}/users (liste allégée)</summary>
public class RoleUserSimpleDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime AssignedAt { get; set; }
}

/// <summary>Détail complet d'un rôle avec tous ses utilisateurs</summary>
public class RoleUsersDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public List<UserInRoleDto> Users { get; set; } = new();
}

public class UserInRoleDto
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeFirstName { get; set; }
    public string? EmployeeLastName { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
// PERMISSIONS CONSTANTS
// ════════════════════════════════════════════════════════════

/// <summary>
/// Constantes string pour les permissions du système.
/// Utilisées dans [HasPermission("...")] et les seeds.
/// </summary>
public static class PermissionsConstants
{
    public const string READ_USERS = "READ_USERS";
    public const string VIEW_USERS = "VIEW_USERS";
    public const string CREATE_USERS = "CREATE_USERS";
    public const string EDIT_USERS = "EDIT_USERS";
    public const string DELETE_USERS = "DELETE_USERS";
    public const string READ_USER = "READ_USER";
    public const string VIEW_USER = "VIEW_USER";
    public const string CREATE_USER = "CREATE_USER";
    public const string EDIT_USER = "EDIT_USER";
    public const string DELETE_USER = "DELETE_USER";

    public const string READ_ROLES = "READ_ROLES";
    public const string VIEW_ROLE = "VIEW_ROLE";
    public const string CREATE_ROLE = "CREATE_ROLE";
    public const string EDIT_ROLE = "EDIT_ROLE";
    public const string DELETE_ROLE = "DELETE_ROLE";
    public const string ASSIGN_ROLES = "ASSIGN_ROLES";
    public const string REVOKE_ROLES = "REVOKE_ROLES";

    public const string READ_PERMISSIONS = "READ_PERMISSIONS";
    public const string MANAGE_PERMISSIONS = "MANAGE_PERMISSIONS";

    public const string READ_COMPANIES = "READ_COMPANIES";
    public const string VIEW_COMPANY = "VIEW_COMPANY";
    public const string CREATE_COMPANY = "CREATE_COMPANY";
    public const string EDIT_COMPANY = "EDIT_COMPANY";
    public const string DELETE_COMPANY = "DELETE_COMPANY";
    public const string VIEW_MANAGED_COMPANIES = "VIEW_MANAGED_COMPANIES";
    public const string VIEW_CABINET_EXPERTS = "VIEW_CABINET_EXPERTS";
    public const string MANAGE_COMPANY_HIERARCHY = "MANAGE_COMPANY_HIERARCHY";

    public const string READ_EMPLOYEES = "READ_EMPLOYEES";
    public const string VIEW_EMPLOYEE = "VIEW_EMPLOYEE";
    public const string CREATE_EMPLOYEE = "CREATE_EMPLOYEE";
    public const string EDIT_EMPLOYEE = "EDIT_EMPLOYEE";
    public const string DELETE_EMPLOYEE = "DELETE_EMPLOYEE";
    public const string VIEW_COMPANY_EMPLOYEES = "VIEW_COMPANY_EMPLOYEES";
    public const string VIEW_SUBORDINATES = "VIEW_SUBORDINATES";
    public const string MANAGE_EMPLOYEE_MANAGER = "MANAGE_EMPLOYEE_MANAGER";

    public const string READ_DEPARTMENTS = "READ_DEPARTMENTS";
    public const string VIEW_DEPARTMENT = "VIEW_DEPARTMENT";
    public const string CREATE_DEPARTMENT = "CREATE_DEPARTMENT";
    public const string EDIT_DEPARTMENT = "EDIT_DEPARTMENT";
    public const string DELETE_DEPARTMENT = "DELETE_DEPARTMENT";

    public const string READ_CONTRACT_TYPES = "READ_CONTRACT_TYPES";
    public const string VIEW_CONTRACT_TYPE = "VIEW_CONTRACT_TYPE";
    public const string CREATE_CONTRACT_TYPE = "CREATE_CONTRACT_TYPE";
    public const string EDIT_CONTRACT_TYPE = "EDIT_CONTRACT_TYPE";
    public const string DELETE_CONTRACT_TYPE = "DELETE_CONTRACT_TYPE";

    public const string READ_COUNTRIES = "READ_COUNTRIES";
    public const string VIEW_COUNTRY = "VIEW_COUNTRY";
    public const string CREATE_COUNTRY = "CREATE_COUNTRY";
    public const string EDIT_COUNTRY = "EDIT_COUNTRY";
    public const string DELETE_COUNTRY = "DELETE_COUNTRY";

    public const string READ_CITIES = "READ_CITIES";
    public const string VIEW_CITY = "VIEW_CITY";
    public const string CREATE_CITY = "CREATE_CITY";
    public const string EDIT_CITY = "EDIT_CITY";
    public const string DELETE_CITY = "DELETE_CITY";

    public const string READ_EMPLOYEE_ADDRESSES = "READ_EMPLOYEE_ADDRESSES";
    public const string VIEW_EMPLOYEE_ADDRESS = "VIEW_EMPLOYEE_ADDRESS";
    public const string CREATE_EMPLOYEE_ADDRESS = "CREATE_EMPLOYEE_ADDRESS";
    public const string UPDATE_EMPLOYEE_ADDRESS = "UPDATE_EMPLOYEE_ADDRESS";
    public const string DELETE_EMPLOYEE_ADDRESS = "DELETE_EMPLOYEE_ADDRESS";
}
