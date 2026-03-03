using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Users.Dtos
{
    public class UserCreateDto
    {
        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Le nom d'utilisateur doit contenir entre 3 et 50 caractères")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
        public required string Password { get; set; }
        public required bool IsActive { get; set; }
    }
}
