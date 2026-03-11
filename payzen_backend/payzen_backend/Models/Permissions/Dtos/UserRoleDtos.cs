using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Permissions.Dtos
{
    // DTO pour assigner un rôle à un utilisateur
    // UTILISATION: POST /api/users/{userId}/roles ou POST /api/roles/{roleId}/users
    public class UserRoleCreateDto
    {
        [Required(ErrorMessage = "L'ID de l'utilisateur est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'utilisateur doit être supérieur à 0")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "L'ID du rôle est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du rôle doit être supérieur à 0")]
        public int RoleId { get; set; }
    }

    // DTO pour lire une association utilisateur-rôle
    // ENRICHISSEMENT: Combine les infos de l'utilisateur et du rôle
    public class UserRoleReadDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }

        // Informations enrichies de l'utilisateur
        public string Username { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        // Informations enrichies du rôle
        public string RoleName { get; set; } = string.Empty;
        public string RoleDescription { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    // DTO simplifié pour lister les rôles d'un utilisateur
    // USAGE: GET /api/users/{id}/roles
    // FOCUS: Détails du rôle uniquement
    public class UserRoleSimpleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoleDescription { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    // DTO pour lister les utilisateurs ayant un rôle spécifique
    // USAGE: GET /api/roles/{id}/users
    // FOCUS: Détails de l'utilisateur uniquement
    public class RoleUserSimpleDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
