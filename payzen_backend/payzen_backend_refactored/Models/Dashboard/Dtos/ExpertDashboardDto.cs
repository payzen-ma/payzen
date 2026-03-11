using System;

namespace payzen_backend.Models.Dashboard.Dtos
{
    public class ExpertDashboardDto
    {
        public int ExpertCompanyId { get; set; }
        public int TotalClients { get; set; }
        public int TotalEmployees { get; set; }
        public DateTimeOffset AsOf { get; set; }
    }
}