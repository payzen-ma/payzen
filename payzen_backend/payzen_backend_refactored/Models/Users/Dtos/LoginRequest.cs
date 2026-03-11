using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Auth
{
    /// <summary>
    /// Represents a request to log in, containing the user's email address and password.
    /// </summary>
    /// <remarks>This class is typically used to encapsulate the credentials required for user authentication.
    /// Both <see cref="Email"/> and <see cref="Password"/> are required fields and must meet the specified validation
    /// criteria.</remarks>
    public class LoginRequest
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        public required string Password { get; set; }
    }
}