namespace payzen_backend.Models.Company.Dtos
{
    public class DepartementReadDto
    {
        public int Id { get; set; }
        public string DepartementName { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
