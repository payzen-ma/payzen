namespace payzen_backend.Models.Company.Dtos
{
    public class CompanyCreateResponseDto
    {
        public CompanyReadDto Company { get; set; } = null!;
        public AdminAccountDto Admin { get; set; } = null!;
    }

    public class AdminAccountDto
    {
        public int EmployeeId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        
        /// <summary>
        /// Mot de passe temporaire (null si l'utilisateur a fourni son propre mot de passe)
        /// </summary>
        public string? Password { get; set; }
        
        public string Message { get; set; } = string.Empty;
    }
}