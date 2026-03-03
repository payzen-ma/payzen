using payzen_backend.Models.Permissions;
using System.Security.Cryptography;

namespace payzen_backend.Models.Users
{
    public class Users
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public string? EmailPersonal { get; set; }
        public required string PasswordHash { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation property pour Employee (IMPORTANT)
        public Employee.Employee? Employee { get; set; }
        // Navigation property pour UsersRoles
        public ICollection<UsersRoles>? UsersRoles { get; set; }
        // Vérifier le mot de passe avec BCrypt
        public bool VerifyPassword(string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
        }
    }
}
