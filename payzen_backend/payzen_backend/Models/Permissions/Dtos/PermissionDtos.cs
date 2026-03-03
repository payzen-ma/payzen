using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Permissions.Dtos
{
    // DTO pour créer une nouvelle permission
    // POURQUOI: Limite les données entrantes à ce qui est strictement nécessaire
    // Évite que l'utilisateur puisse définir Id, CreatedAt, etc.
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

    // DTO pour lire une permission (réponse API)
    // POURQUOI: Ne retourne que les informations publiques et utiles
    // Cache les données sensibles comme CreatedBy, UpdatedBy, DeletedBy
    public class PermissionReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Resource { get; set; } = string.Empty;
        public string? Action { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // DTO pour mettre à jour une permission existante
    // POURQUOI: Toutes les propriétés sont optionnelles (nullable)
    // Permet des mises à jour partielles (PATCH-like behavior)
    public class PermissionUpdateDto
    {
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Le nom doit contenir entre 3 et 100 caractères")]
        public string? Name { get; set; }

        [StringLength(500, MinimumLength = 10, ErrorMessage = "La description doit contenir entre 10 et 500 caractères")]
        public string? Description { get; set; }
    }
}
