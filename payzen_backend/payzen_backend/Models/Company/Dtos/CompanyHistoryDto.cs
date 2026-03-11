using System.Collections.Generic;

namespace payzen_backend.Models.Company.Dtos
{
    /// <summary>
    /// DTO pour l'historique d'une entreprise expos� par l'API (/api/companies/{companyId}/history)
    /// </summary>
    public class CompanyHistoryDto
    {
        public string Type { get; set; } = null!; // "company" | "employee" | autre
        public string Title { get; set; } = null!;
        public string Date { get; set; } = null!; // format lisible (ex: "2025-12-23")
        public string Description { get; set; } = null!;
        public Dictionary<string, object?>? Details { get; set; }
        public ModifiedByDto? ModifiedBy { get; set; }
        public string Timestamp { get; set; } = null!; // ISO 8601 (o)
    }

    public class ModifiedByDto
    {
        public string? Name { get; set; }
        public string? Role { get; set; }
    }
}
