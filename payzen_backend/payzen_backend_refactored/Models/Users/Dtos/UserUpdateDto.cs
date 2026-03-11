using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Users.Dtos
{
    public class UserUpdateDto
    {

        [StringLength(50, MinimumLength = 3, ErrorMessage = "Le nom d'utilisateur doit contenir entre 3 et 50 caractères")]
        public string? Username { get; set; }

        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
        public string? Email { get; set; }

        [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
        public string? Password { get; set; }

        public bool? IsActive { get; set; }
    }
}
