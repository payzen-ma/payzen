using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    /// <summary>
    /// DTO pour cr�er une absence
    /// </summary>
    public class EmployeeAbsenceCreateDto
    {
        [Required(ErrorMessage = "L'ID de l'employ� est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'employ� doit �tre valide")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "La date d'absence est requise")]
        public DateOnly AbsenceDate { get; set; }

        [Required(ErrorMessage = "Le type de dur�e est requis")]
        public AbsenceDurationType DurationType { get; set; }

        // Pour HalfDay
        public bool? IsMorning { get; set; }

        // Pour Hourly
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        [Required(ErrorMessage = "Le type d'absence est requis")]
        [StringLength(50, ErrorMessage = "Le type d'absence ne peut pas d�passer 50 caract�res")]
        public required string AbsenceType { get; set; }

        [StringLength(500, ErrorMessage = "La raison ne peut pas d�passer 500 caract�res")]
        public string? Reason { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// DTO pour lire une absence
    /// </summary>
    public class EmployeeAbsenceReadDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeFirstName { get; set; } = string.Empty;
        public string EmployeeLastName { get; set; } = string.Empty;
        public string EmployeeFullName { get; set; } = string.Empty;
        public DateOnly AbsenceDate { get; set; }
        public string AbsenceDateFormatted { get; set; } = string.Empty;
        public AbsenceDurationType DurationType { get; set; }
        public string DurationTypeDescription { get; set; } = string.Empty;
        public bool? IsMorning { get; set; }
        public string? HalfDayDescription { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string AbsenceType { get; set; } = string.Empty;
        public string? Reason { get; set; }

        // Nouveaux champs de d�cision
        public AbsenceStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public DateTime? DecisionAt { get; set; }
        public int? DecisionBy { get; set; }
        public string? DecisionByName { get; set; }
        public string? DecisionComment { get; set; }

        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
    }
    public class EmployeeAbsenceCancellationDto
    {
        [StringLength(500, ErrorMessage = "La raison ne peut pas d�passer 500 caract�res")]
        public string? Reason { get; set; }
    }
    /// <summary>
    /// DTO pour les statistiques d'absences
    /// </summary>
    public class EmployeeAbsenceStatsDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int? EmployeeId { get; set; }
        public string? EmployeeFullName { get; set; }
        public int TotalAbsences { get; set; }
        public int FullDayAbsences { get; set; }
        public int HalfDayAbsences { get; set; }
        public int HourlyAbsences { get; set; }

        // Statistiques par statut
        public int SubmittedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int CancelledCount { get; set; }

        public Dictionary<string, int> AbsencesByType { get; set; } = new();
        public Dictionary<string, int> AbsencesByMonth { get; set; } = new();
        public DateTimeOffset GeneratedAt { get; set; }
    }

    /// <summary>
    /// DTO pour mettre � jour une absence
    /// </summary>
    public class EmployeeAbsenceUpdateDto
    {
        public DateOnly? AbsenceDate { get; set; }
        public AbsenceDurationType? DurationType { get; set; }
        public bool? IsMorning { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        [StringLength(50, ErrorMessage = "Le type d'absence ne peut pas d�passer 50 caract�res")]
        public string? AbsenceType { get; set; }

        [StringLength(500, ErrorMessage = "La raison ne peut pas d�passer 500 caract�res")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO pour prendre une d�cision sur une absence (Approuver/Rejeter)
    /// </summary>
    public class EmployeeAbsenceDecisionDto
    {
        [Required(ErrorMessage = "Le statut de d�cision est requis")]
        public AbsenceStatus Status { get; set; }

        [StringLength(1000, ErrorMessage = "Le commentaire ne peut pas d�passer 1000 caract�res")]
        public string? DecisionComment { get; set; }
    }

    /// <summary>
    /// DTO pour approuver une absence (commentaire optionnel)
    /// </summary>
    public class EmployeeAbsenceApprovalDto
    {
        [StringLength(1000, ErrorMessage = "Le commentaire ne peut pas d�passer 1000 caract�res")]
        public string? Comment { get; set; }
    }

    /// <summary>
    /// DTO pour rejeter une absence (raison requise)
    /// </summary>
    public class EmployeeAbsenceRejectionDto
    {
        [Required(ErrorMessage = "La raison du rejet est requise")]
        [StringLength(1000, MinimumLength = 3, ErrorMessage = "La raison doit contenir entre 3 et 1000 caract�res")]
        public required string Reason { get; set; }
    }
}