namespace payzen_backend.Models.Company.Dtos
{
    public class JobPositionReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}