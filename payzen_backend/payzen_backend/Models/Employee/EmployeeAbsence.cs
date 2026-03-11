using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace payzen_backend.Models.Employee
{
    public class EmployeeAbsence
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public DateOnly AbsenceDate { get; set; }

        public AbsenceDurationType DurationType { get; set; }
        // FullDay | HalfDay | Hourly

        // Decision d'absence
        public AbsenceStatus Status { get; set; }
        public DateTimeOffset? DecisionAt { get; set; }
        public int? DecisionBy { get; set; }
        public string? DecisionComment { get; set; }

        // ---- Demi-journée ----
        public bool? IsMorning { get; set; }
        // true = matin, false = après-midi

        // ---- Tranche horaire ----
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        public string AbsenceType { get; set; } = null!;
        public string? Reason { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public int CreatedBy { get; set; }     
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? DeletedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
    /// <summary>
    /// DTO pour annuler une absence
    /// </summary>

    public enum AbsenceDurationType
    {
        FullDay = 1,
        HalfDay = 2,
        Hourly = 3
    }

    public enum AbsenceStatus
    {
        Draft = 0,
        Submitted = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4,
        Expired = 5
    }
}
