namespace payzen_backend.Models.Dashboard.Dtos
{
    /// <summary>
    /// Repr�sentation d'un employ� dans le dashboard
    /// </summary>
    public class EmployeeDashboardItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string statuses { get; set; } = string.Empty;
        public string NameFr { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public int MissingDocuments { get; set; }
        public string ContractType { get; set; } = string.Empty; // 'CDI' | 'CDD' | 'Stage'
        public string? Manager { get; set; }
    }
}
