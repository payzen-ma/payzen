namespace payzen_backend.Models.Dashboard.Dtos
{
    /// <summary>
    /// R�ponse compl�te du dashboard avec statistiques et liste des employ�s
    /// </summary>
    public class DashboardResponseDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public List<EmployeeDashboardItemDto> Employees { get; set; } = new();
        public List<string> Departements { get; set; } = new();
        public List<string> statuses { get; set; } = new();
    }
}
