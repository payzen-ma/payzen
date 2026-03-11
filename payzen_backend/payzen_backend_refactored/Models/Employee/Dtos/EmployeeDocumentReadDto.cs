namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeDocumentReadDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}