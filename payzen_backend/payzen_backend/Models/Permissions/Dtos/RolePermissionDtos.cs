using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Permissions.Dtos
{
    // DTO pour assigner une permission à un rôle
    // SIMPLICITÉ: Seulement les IDs nécessaires pour créer la liaison
    // Les IDs peuvent aussi venir de la route (ex: POST /roles/{roleId}/permissions/{permissionId})
    public class RolePermissionCreateDto
    {
        [Required(ErrorMessage = "L'ID du rôle est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du rôle doit être supérieur à 0")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "L'ID de la permission est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la permission doit être supérieur à 0")]
        public int PermissionId { get; set; }
    }

    // DTO pour lire une association rôle-permission
    // ENRICHISSEMENT: Inclut les détails du rôle ET de la permission
    // POURQUOI: Évite au client de faire des appels multiples pour obtenir les détails
    public class RolePermissionReadDto
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        
        // Informations enrichies du rôle
        public string RoleName { get; set; } = string.Empty;
        
        // Informations enrichies de la permission
        public string PermissionName { get; set; } = string.Empty;
        public string PermissionDescription { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
    }

    // DTO simplifié pour lister les permissions d'un rôle
    // USAGE: GET /api/roles/{id}/permissions
    // LÉGÈRETÉ: Seulement les infos essentielles de la permission
    public class RolePermissionSimpleDto
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string PermissionDescription { get; set; } = string.Empty;
    }
}
