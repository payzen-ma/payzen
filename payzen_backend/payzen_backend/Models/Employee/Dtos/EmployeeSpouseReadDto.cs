namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeSpouseReadDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeFirstName { get; set; } = string.Empty;
        public string EmployeeLastName { get; set; } = string.Empty;
        public string EmployeeFullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public int? GenderId { get; set; }
        public string? GenderName { get; set; }
        public string? CinNumber { get; set; }
        public DateTime? MarriageDate { get; set; }
        public bool IsDependent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
