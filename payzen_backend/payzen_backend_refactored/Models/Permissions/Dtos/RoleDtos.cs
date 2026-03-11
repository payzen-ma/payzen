using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Permissions.Dtos
{
    // DTO pour créer un nouveau rôle
    // STRUCTURE: Identique à PermissionCreateDto car les modèles sont similaires
    public class RoleCreateDto
    {
        [Required(ErrorMessage = "Le nom du rôle est requis")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Le nom du rôle doit contenir entre 2 et 50 caractères")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "La description est requise")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "La description doit contenir entre 10 et 500 caractères")]
        public required string Description { get; set; }
    }

    // DTO pour lire un rôle
    // UTILISATION: Retourné par les endpoints GET
    public class RoleReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // DTO pour mettre à jour un rôle
    // FLEXIBILITÉ: Permet de modifier Name ou Description indépendamment
    public class RoleUpdateDto
    {
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Le nom du rôle doit contenir entre 2 et 50 caractères")]
        public string? Name { get; set; }

        [StringLength(500, MinimumLength = 10, ErrorMessage = "La description doit contenir entre 10 et 500 caractères")]
        public string? Description { get; set; }
    }
}
