namespace payzen_backend.Models.Employee.Dtos
{
    /// <summary>
    /// DTO simplifi� pour la liste des employ�s actifs d'autres entreprises
    /// </summary>
    public class EmployeeSimpleDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public List<string>? RoleNames { get; set; }
        public string? statuses { get; set; }
    }
}