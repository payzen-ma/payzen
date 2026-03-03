namespace payzen_backend.Models.Company
{
    /// <summary>
    /// Calendrier de travail par entreprise (jours et horaires)
    /// </summary>
    public class WorkingCalendar
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int DayOfWeek { get; set; } // 0=Dimanche, 1=Lundi, ..., 6=Samedi
        public bool IsWorkingDay { get; set; } = true;
        public TimeSpan? StartTime { get; set; } // Ex: 09:00
        public TimeSpan? EndTime { get; set; } // Ex: 18:00

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Company? Company { get; set; } = null!;
    }
}
