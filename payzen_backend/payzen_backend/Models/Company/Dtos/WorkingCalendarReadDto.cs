namespace payzen_backend.Models.Company.Dtos
{
    public class WorkingCalendarReadDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int DayOfWeek { get; set; }
        public string DayOfWeekName { get; set; } = string.Empty; // Lundi, Mardi, etc.
        public bool IsWorkingDay { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}